git fetch origin
git fetch origin %1:%1
git branch --set-upstream-to=origin/%1 %1
git switch %1
git pull
git status