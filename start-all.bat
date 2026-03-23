@echo off
echo ========================================
echo  NetEase Lyrics Bar - 启动脚本
echo ========================================
echo.
echo [1/2] 启动 API 服务器...
start /min cmd /c "cd /d %~dp0api-server && npm start"
echo 等待 API 服务器启动...
timeout /t 3 /nobreak > nul
echo.
echo [2/2] 启动歌词栏...
cd /d %~dp0NetEaseLyricsBar
dotnet run
pause
