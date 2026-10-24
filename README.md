# 4UP

**Windows 桌面健康浮窗** —— 喝水、起身、远眺（护眼）、提肛（盆底肌）四槽衰减提醒。安静、无通知轰炸、不联网。

灵感致谢：[xfey/1up](https://github.com/xfey/1up)（macOS，MIT）。4UP 是其机制在 Windows 上的独立重写（C# WinForms，单可执行，无外部依赖），并非移植其代码。

## 功能

- **四槽状态条**：起身 45 分 / 喝水 30 分 / 远眺 20 分 / 提肛 60 分，随时间线性衰减，托盘图标即迷你状态条
- **补给**：点击条目或「＋」补满；离开键盘 ≥15 分钟、锁屏、睡眠视为休息，全部回满
- **连击**：跨天坚持计数，全空一天则清零重来
- **两种模式**：常驻桌面浮窗 / 仅空槽时弹出 20 秒（托盘右键 → 设置）
- **自定义提醒项**：1-10 个，名称与周期（5-480 分钟）随意改
- **暂停**：托盘右键暂停 1 小时（开会/演示场景）
- **安静哲学**：无弹窗通知轰炸；空槽仅托盘气泡轻提示，隐身模式下才弹出浮窗

## 构建（Windows 10/11，无需安装任何 SDK）

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

使用系统自带的 .NET Framework 4 csc.exe 编译为单文件 `4up.exe`（C#5 语法，anycpu，带自绘图标）。

## 安装为桌面应用

```powershell
powershell -ExecutionPolicy Bypass -File install.ps1
```

安装到 `%LOCALAPPDATA%\Programs\4UP`，创建桌面/开始菜单快捷方式，注册登录自启 + 5 分钟看门狗计划任务。卸载：

```powershell
powershell -ExecutionPolicy Bypass -File uninstall.ps1          # 保留数据
powershell -ExecutionPolicy Bypass -File uninstall.ps1 -PurgeData # 连数据删除
```

## 使用

- 托盘图标：左键开合浮窗，右键菜单（设置/暂停/自启/关于/退出）
- 浮窗可拖动，位置记忆；Esc 或点托盘隐藏
- 提醒项整行可点击补满；悬停显示剩余时间
- 文案改 `copy.txt`（`[节]` + 每行一句，随机轮播），热改即时生效？——重启进程生效

## 数据与排障

- 状态：`%APPDATA%\Life4Up\state.json`（明文 JSON，可备份）
- 日志：同目录 `life.log`（心跳/提醒事件）、`crash.log`（异常全栈）

## License

MIT