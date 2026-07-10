@echo off
setlocal EnableDelayedExpansion
set APP="bin\Editor_x64.exe"
if exist %APP% (
	start "" %APP% -video_app auto -video_refresh 0 -video_debug 0 -main_window_size 1920 1080 -main_window_resizable 1 -main_window_fullscreen 0 -render_vsync 0 -video_offscreen 0 -sound_app auto -data_path ../data/ -microprofile_enabled 0 -extern_plugin UnigineFbxImporter,UnigineGLTFImporter,UnigineFbxExporter -console_command "config_autosave 1 && world_load \"DvigAtel\""
) else (
	set MESSAGE=%APP% not found"
	echo !MESSAGE!
)