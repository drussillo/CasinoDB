@echo off

set DBPATH="C:\\ProgramData\\CasinoDB\\"

if not exist "%DBPATH%" mkdir "%DBPATH%"

copy "..\Database\Casino.mdf" "%DBPATH%"
copy "..\Database\Casino_log.ldf" "%DBPATH%"

if exist "%DBPATH%\Casino.mdf" if exist "%DBPATH%\Casino_log.ldf" (
    echo File successfully saved at %DBPATH%\Casino.mdf.
    echo File successfully saved at %DBPATH%\Casino_log.ldf.
    echo Installation Succeeded.
) else (
    echo Installation Failed.
)
