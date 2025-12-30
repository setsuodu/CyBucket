# Cygames全家桶

> 解决优雅+高效问题；
> Template + Package + 加密Dll核心，拼项目；

# Steps:

1. 配置包管理：
	1. OpenUPM
	2. NugetForUnity
	```
	{
	  "scopedRegistries": [
		{
		  "name": "OpenUPM",
		  "url": "https://package.openupm.com",
		  "scopes": [
			"com.cysharp",
			"jp.hadashikick",  // VContainer
			"com.openupm",     // 可选，覆盖更多
			"com.github-glitchenzo.nugetforunity"
		  ]
		}
	  ],
	  "dependencies": {
		"com.cysharp.messagepipe": "1.8.1",
		"com.cysharp.messagepipe.vcontainer": "1.8.1",
		"com.cysharp.unitask": "2.5.10",
		"com.github-glitchenzo.nugetforunity": "4.5.0",
		"jp.hadashikick.vcontainer": "1.17.0",
		// 其他Unity官方（示例）
		"com.unity.ugui": "1.0.0",
		"com.unity.ide.visualstudio": "2.0.26"
	  }
	}
	```
	- [ ] NugetForUnity
		- OpenUPM⚙️"com.github-glitchenzo.nugetforunity":4.5.0
	- [ ] UniTask
		- OpenUPM⚙️com.cysharp.unitask:2.5.10
	- [ ] VContainer
		- OpenUPM⚙️jp.hadashikick.vcontainer:1.17.0
	- [ ] MessagePipe
		- OpenUPM⚙️com.cysharp.messagepipe:1.8.1
		- OpenUPM⚙com.cysharp.messagepipe.vcontainer:1.8.1
	- [ ] R3
		- OpenUPM⚙各种not found
		- NuGet⚙R3:1.3.0
	- [ ] MemoryPack
		- OpenUPM⚙️各种not found
		- NuGet⚙MemoryPack:1.21.4