@echo off

:: Start the Front end
wt -w 0 nt --tabColor "#00FF00" --title "TallyJ Web" --suppressApplicationTitle -p "Command Prompt" -d "Frontend" ^
cmd /k "echo Waiting to let backend services get ready... && timeout /t 10 && npm install && npm start"

:: start the Backend
wt -w 0 nt --tabColor "#6699FF" --title "TallyJ .NET" --suppressApplicationTitle -p "Command Prompt" -d "Backend" ^
cmd /k dotnet watch


