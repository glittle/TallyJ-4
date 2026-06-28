@echo off

:: Start an empty tab
wt -w 0 nt --tabColor "#669966" --title "TallyJ Empty" --suppressApplicationTitle -p "Command Prompt" -d "." ^
cmd /k "echo In the root folder."

::use Timeout to put both commands into the same Terminal window. Comment out to open into separate windows.
timeout /t 2  

:: start the Backend
wt -w 0 nt --tabColor "#6699FF" --title "TallyJ .NET" --suppressApplicationTitle -p "Command Prompt" -d "Backend" ^
cmd /k dotnet watch

::use Timeout to put both commands into the same Terminal window. Comment out to open into separate windows.
timeout /t 2  

:: Start the Front end
wt -w 0 nt --tabColor "#00FF00" --title "TallyJ Web" --suppressApplicationTitle -p "Command Prompt" -d "Frontend" ^
cmd /k "echo Waiting to let backend services get ready... && timeout /t 15 && npm install && s"


