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
		// Cysharp 全家桶核心
		"com.cysharp.unitask": "2.5.10",                 // 或不写版本，让自动最新
		"com.cysharp.messagepipe": "1.9.0",
		"com.cysharp.messagepipe.vcontainer": "1.9.0",
		"com.cysharp.memorypack": "1.10.0",
		"com.cysharp.zstring": "2.6.0",

		// 强烈推荐加这些
		"com.cysharp.r3": "1.0.0",                        // 取代UniRx
		"com.cysharp.observablecollections": "1.0.0",    // 动态UI列表
		"com.cysharp.mastermemory": "1.0.0",             // 表查询加速

		// DI框架（非Cysharp，但必配）
		"jp.hadashikick.vcontainer": "1.17.0",

		// 其他Unity官方（示例）
		"com.unity.ugui": "1.0.0",
		"com.unity.ide.visualstudio": "2.0.26"
	  }
	}
	```
2. 