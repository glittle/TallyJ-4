<script setup lang="ts"></script>

<template>
  <div class="people-import-file-guide">
    <p>{{ $t("people.import.fileGuideIntro") }}</p>
    <ul class="guide-list">
      <li>
        <strong>{{ $t("people.import.fileGuideRequiredLabel") }}</strong>
        {{ $t("people.import.fileGuideRequired") }}
      </li>
      <li>
        <strong>{{ $t("people.import.fileGuideOptionalLabel") }}</strong>
        {{ $t("people.import.fileGuideOptional") }}
      </li>
    </ul>
    <p>{{ $t("people.import.fileGuideEligibility") }}</p>
    <el-collapse>
      <el-collapse-item
        :title="$t('people.import.eligibilityValues')"
        name="eligibility"
      >
        <ul class="eligibility-list">
          <li>
            <strong>Eligible</strong> —
            {{ $t("people.import.eligibleDesc") }}
          </li>
          <li>
            <strong>Ineligible</strong> —
            {{ $t("people.import.ineligibleDesc") }}
          </li>
          <li>
            <strong>Under Age</strong> —
            {{ $t("people.import.underAgeDesc") }}
          </li>
          <li>
            <strong>Duplicate</strong> —
            {{ $t("people.import.duplicateDesc") }}
          </li>
        </ul>
      </el-collapse-item>
    </el-collapse>
  </div>
</template>

<style lang="less">
.people-import-file-guide {
  p {
    margin: 0 0 12px;
    color: var(--el-text-color-regular);
  }

  .guide-list {
    margin: 0 0 12px;
    padding-left: 20px;

    li {
      margin-bottom: 6px;
    }
  }

  .eligibility-list {
    margin: 0;
    padding-left: 20px;

    li {
      margin-bottom: 6px;
    }
  }
}
</style>
