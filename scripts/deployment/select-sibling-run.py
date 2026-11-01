#!/usr/bin/env python3
"""Pick the other pipeline's artifact for a combined UAT deploy.

Azure DevOps Builds List returns sourceVersion on each build and does not
accept it as a query filter, so callers list recent builds and match the
commit here. DownloadPipelineArtifact@2 latestFromBranch only sees a
completed run whose result is succeeded, which misses the other half of
the push that started both pipelines.

Waiting until that other run finishes deadlocks: each deploy is still part
of the run the other side would be waiting on. An in-progress run can be
downloaded by id once its artifact is published.
"""

from __future__ import annotations

import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

API_VERSION = "7.1"
ACTIVE_STATUSES = {"notstarted", "inprogress", "postponed", "cancelling", "none", ""}

# REST enum integers, used when the service returns numbers instead of names.
STATUS_BY_INT = {
    0: "none",
    1: "inprogress",
    2: "completed",
    4: "cancelling",
    8: "postponed",
    32: "notstarted",
}
RESULT_BY_INT = {
    0: "none",
    2: "succeeded",
    4: "partiallysucceeded",
    8: "failed",
    32: "canceled",
}


class SiblingError(RuntimeError):
    pass


def _norm_token(value, by_int):
    if value is None:
        return ""
    if isinstance(value, int):
        return by_int.get(value, str(value))
    text = str(value).strip()
    if text.isdigit():
        return by_int.get(int(text), text.lower())
    text = text.lower().replace("_", "")
    if text == "cancelled":
        return "canceled"
    return text


def status_of(build):
    return _norm_token(build.get("status"), STATUS_BY_INT)


def result_of(build):
    return _norm_token(build.get("result"), RESULT_BY_INT)


def same_commit(build, commit):
    return (build.get("sourceVersion") or "").lower() == commit.lower()


def newest(builds):
    return max(builds, key=lambda build: int(build.get("id") or 0))


def classify(builds, commit, published_ids):
    """Decide what to do with one snapshot of the other definition.

    published_ids holds run ids already known to have published the artifact.
    Returns a dict: action is wait, specific, or latest.
    """
    mine = [build for build in builds if same_commit(build, commit)]
    if not mine:
        return {
            "action": "latest",
            "run_id": None,
            "warning": False,
            "message": (
                "No run of the other pipeline is building this commit. "
                "Using the latest successful artifact on main."
            ),
        }

    active = [build for build in mine if status_of(build) in ACTIVE_STATUSES]
    if active:
        run = newest(active)
        run_id = int(run["id"])
        if run_id in published_ids:
            return {
                "action": "specific",
                "run_id": run_id,
                "warning": False,
                "message": (
                    f"Other pipeline run {run_id} is {status_of(run) or 'queued'} "
                    "and has published its artifact. Downloading that run. "
                    "Not waiting for its deploy to finish, which would deadlock."
                ),
            }
        return {
            "action": "wait",
            "run_id": run_id,
            "warning": False,
            "message": (
                f"Other pipeline run {run_id} is {status_of(run) or 'queued'} "
                "on this commit and has not published its artifact yet."
            ),
        }

    succeeded = [
        build
        for build in mine
        if status_of(build) == "completed" and result_of(build) == "succeeded"
    ]
    if succeeded:
        run = newest(succeeded)
        run_id = int(run["id"])
        if run_id in published_ids:
            return {
                "action": "specific",
                "run_id": run_id,
                "warning": False,
                "message": f"Other pipeline run {run_id} succeeded on this commit. Downloading that run.",
            }
        return {
            "action": "latest",
            "run_id": None,
            "warning": True,
            "message": (
                f"Other pipeline run {run_id} succeeded on this commit but did not publish "
                "the expected artifact. Falling back to the latest successful run on main."
            ),
        }

    run = newest(mine)
    run_id = int(run["id"])
    result = result_of(run) or "unknown"
    return {
        "action": "latest",
        "run_id": None,
        "warning": True,
        "message": (
            f"Other pipeline run {run_id} on this commit finished with result {result}. "
            "A run whose build passed and whose deploy failed is not a successful run. "
            "Falling back to the latest successful run on main."
        ),
    }


def resolve(fetch_builds, artifact_published, commit, timeout_s, poll_s, sleep, monotonic):
    deadline = monotonic() + timeout_s
    while True:
        builds = fetch_builds()
        active_ids = [
            int(build["id"])
            for build in builds
            if same_commit(build, commit) and status_of(build) != "completed"
        ]
        succeeded_ids = [
            int(build["id"])
            for build in builds
            if same_commit(build, commit)
            and status_of(build) == "completed"
            and result_of(build) == "succeeded"
        ]
        published = set()
        for run_id in active_ids + succeeded_ids:
            if artifact_published(run_id):
                published.add(run_id)
        decision = classify(builds, commit, published)
        if decision["action"] != "wait":
            return decision
        if monotonic() >= deadline:
            run_id = decision["run_id"]
            return {
                "action": "latest",
                "run_id": None,
                "warning": True,
                "message": (
                    f"Timed out after {timeout_s} seconds waiting for other pipeline run {run_id} "
                    "to publish its artifact. Falling back to the latest successful run on main."
                ),
            }
        print(decision["message"], flush=True)
        sleep(poll_s)


class HttpStatusError(SiblingError):
    def __init__(self, status, body):
        super().__init__(f"Azure DevOps REST call failed with HTTP {status}: {body}")
        self.status = status


def _request_json(url, token):
    request = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            payload = response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        body = error.read().decode("utf-8", errors="replace")[:500]
        raise HttpStatusError(error.code, body) from None
    except urllib.error.URLError as error:
        raise SiblingError(f"Azure DevOps REST call failed: {error.reason}") from None
    return json.loads(payload)


def _builds_url(base, definition, branch):
    # Keep the $ in $top. urlencode would otherwise turn it into %24top.
    query = urllib.parse.urlencode(
        [
            ("definitions", str(definition)),
            ("branchName", branch),
            ("queryOrder", "queueTimeDescending"),
            ("$top", "50"),
            ("api-version", API_VERSION),
        ],
        safe="$",
    )
    return f"{base}/_apis/build/builds?{query}"


def _artifacts_url(base, build_id):
    return f"{base}/_apis/build/builds/{build_id}/artifacts?api-version={API_VERSION}"


def emit(decision):
    mode = "specific" if decision["action"] == "specific" else "latestFromBranch"
    run_id = decision["run_id"] if decision["action"] == "specific" else ""
    if decision["warning"]:
        print(f"##vso[task.logissue type=warning]{decision['message']}", flush=True)
    print(decision["message"], flush=True)
    print(f"##vso[task.setvariable variable=otherRunVersion]{mode}", flush=True)
    print(f"##vso[task.setvariable variable=otherRunId]{run_id}", flush=True)


def main():
    token = os.environ.get("SYSTEM_ACCESSTOKEN") or ""
    if not token:
        raise SiblingError(
            "SYSTEM_ACCESSTOKEN is empty. Map the secret System.AccessToken into the step environment."
        )
    collection = (os.environ.get("SYSTEM_COLLECTIONURI") or "").rstrip("/")
    project = os.environ.get("SYSTEM_TEAMPROJECTID") or ""
    definition = os.environ.get("OTHER_PIPELINE_ID") or ""
    artifact = os.environ.get("OTHER_ARTIFACT") or ""
    commit = os.environ.get("BUILD_SOURCEVERSION") or ""
    branch = os.environ.get("BUILD_SOURCEBRANCH") or "refs/heads/main"
    if not collection or not project or not definition or not artifact or not commit:
        raise SiblingError("Missing collection, project, pipeline id, artifact name, or source version.")
    if not definition.isdigit():
        raise SiblingError(f"Other pipeline id must be numeric, got {definition!r}.")

    timeout_s = int(os.environ.get("SIBLING_TIMEOUT_SECONDS") or "2400")
    poll_s = int(os.environ.get("SIBLING_POLL_SECONDS") or "30")
    base = f"{collection}/{urllib.parse.quote(project)}"

    def fetch_builds():
        payload = _request_json(_builds_url(base, definition, branch), token)
        return payload.get("value") or []

    def artifact_published(run_id):
        try:
            payload = _request_json(_artifacts_url(base, run_id), token)
        except HttpStatusError as error:
            if error.status == 404:
                return False
            raise
        return any(item.get("name") == artifact for item in (payload.get("value") or []))

    decision = resolve(
        fetch_builds,
        artifact_published,
        commit,
        timeout_s,
        poll_s,
        time.sleep,
        time.monotonic,
    )
    emit(decision)


def self_test():
    commit = "abc123"
    other = {"id": 10, "sourceVersion": commit, "status": "inProgress", "result": None}
    done = {"id": 8, "sourceVersion": commit, "status": "completed", "result": "succeeded"}
    failed = {"id": 11, "sourceVersion": commit, "status": "completed", "result": "failed"}
    partial = {"id": 12, "sourceVersion": commit, "status": 2, "result": 4}
    older = {"id": 7, "sourceVersion": "older", "status": "completed", "result": "succeeded"}

    no_sibling = classify([older], commit, set())
    assert no_sibling["action"] == "latest" and no_sibling["warning"] is False

    waiting = classify([other, older], commit, set())
    assert waiting["action"] == "wait" and waiting["run_id"] == 10

    ready = classify([other], commit, {10})
    assert ready["action"] == "specific" and ready["run_id"] == 10

    queued = classify(
        [{"id": 15, "sourceVersion": commit, "status": 32, "result": 0}],
        commit,
        set(),
    )
    assert queued["action"] == "wait" and queued["run_id"] == 15

    queued_string = classify(
        [{"id": 16, "sourceVersion": commit.upper(), "status": "1", "result": "0"}],
        commit,
        set(),
    )
    assert queued_string["action"] == "wait" and queued_string["run_id"] == 16

    succeeded = classify([done, older], commit, {8})
    assert succeeded["action"] == "specific" and succeeded["run_id"] == 8

    missing_artifact = classify([done], commit, set())
    assert missing_artifact["action"] == "latest" and missing_artifact["warning"] is True

    failed_run = classify([failed, done], commit, {8})
    # Newest completed failure is ignored while an older success on this commit exists.
    assert failed_run["action"] == "specific" and failed_run["run_id"] == 8

    only_failed = classify([failed], commit, {11})
    assert only_failed["action"] == "latest" and only_failed["warning"] is True
    assert "not a successful run" in only_failed["message"]

    partial_only = classify([partial], commit, {12})
    assert partial_only["action"] == "latest" and partial_only["warning"] is True

    active_retry = classify([done, other], commit, {8, 10})
    assert active_retry["action"] == "specific" and active_retry["run_id"] == 10

    # Timeout falls back even while the sibling is still queued.
    clock = {"now": 0}

    def monotonic():
        return clock["now"]

    def sleep(seconds):
        clock["now"] += seconds

    polls = {"n": 0}

    def fetch():
        polls["n"] += 1
        return [dict(other)]

    def never_published(_run_id):
        return False

    timed_out = resolve(fetch, never_published, commit, timeout_s=60, poll_s=30, sleep=sleep, monotonic=monotonic)
    assert timed_out["action"] == "latest" and timed_out["warning"] is True
    assert "Timed out" in timed_out["message"]
    assert polls["n"] >= 2

    # Artifact appears on the second poll, so we take that run and do not wait out the timeout.
    clock["now"] = 0
    state = {"polls": 0}

    def fetch_live():
        state["polls"] += 1
        return [dict(other)]

    def publish_on_second(run_id):
        return state["polls"] >= 2 and run_id == 10

    ready_later = resolve(
        fetch_live,
        publish_on_second,
        commit,
        timeout_s=2400,
        poll_s=30,
        sleep=sleep,
        monotonic=monotonic,
    )
    assert ready_later["action"] == "specific" and ready_later["run_id"] == 10
    print("select-sibling-run self-test ok")


if __name__ == "__main__":
    if "--self-test" in sys.argv:
        self_test()
    else:
        try:
            main()
        except SiblingError as error:
            print(f"##vso[task.logissue type=error]{error}", flush=True)
            print(error, file=sys.stderr)
            sys.exit(1)
