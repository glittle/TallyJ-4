Set-Location 'C:\Dev\TallyJ\v4\repo'

# Step 1: Create new backend tree without the invalid file
$backendTreeHash = '0efdcb1bbbda126f94714c065de390c47f5351a9'
$lines = git ls-tree $backendTreeHash
$filtered = $lines | Where-Object { $_ -notmatch 'openapi' }
Write-Host "Backend: $($lines.Count) entries -> $($filtered.Count) after filter"

$newBackendTree = ($filtered | git mktree)
Write-Host "New backend tree: $newBackendTree"

# Step 2: Create new root tree replacing the backend subtree hash
$rootLines = git ls-tree 44aef64
$newRootLines = $rootLines | ForEach-Object {
    if ($_ -match "\tbackend$") {
        $_ -replace $backendTreeHash, $newBackendTree
    } else {
        $_
    }
}
$newRootTree = ($newRootLines | git mktree)
Write-Host "New root tree: $newRootTree"

# Step 3: Create new commit preserving original metadata
$badCommit = '44aef64'
$parentOfBad = (git rev-parse "${badCommit}^")
Write-Host "Parent of bad commit: $parentOfBad"

$env:GIT_AUTHOR_NAME    = (git log -1 --format="%an" $badCommit)
$env:GIT_AUTHOR_EMAIL   = (git log -1 --format="%ae" $badCommit)
$env:GIT_AUTHOR_DATE    = (git log -1 --format="%ai" $badCommit)
$env:GIT_COMMITTER_NAME  = $env:GIT_AUTHOR_NAME
$env:GIT_COMMITTER_EMAIL = $env:GIT_AUTHOR_EMAIL
$env:GIT_COMMITTER_DATE  = $env:GIT_AUTHOR_DATE

$msg = (git log -1 --format="%s" $badCommit)
$newCommit = (git commit-tree $newRootTree -p $parentOfBad -m $msg)
Write-Host "New commit: $newCommit"

# Step 4: Point branch to new commit
git branch -f copilot/improve-main-dashboard $newCommit
Write-Host "Done! Now try: git switch copilot/improve-main-dashboard"
