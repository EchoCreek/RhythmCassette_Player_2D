# 律动磁带 (Rhythm Cassette)

> 一款让你“把玩”音乐的、充满复古情怀与物理交互感的个性化数字音乐玩具。

---

## 已知问题名单：

- [ ] 生硬的谱面跳动效果

---

## **源码的下载方式**：

使用 `git clone` 命令。请按照以下步骤操作：

1. 确保你的电脑已经安装了 [Git](https://git-scm.com/downloads) 和 [Git LFS](https://git-lfs.github.com/)。

2. 打开终端（命令行工具），运行以下命令：

   ```
   # 第一步：为你的 Git 启用 LFS 功能 (在电脑上只需执行一次)
   git lfs install
   
   # 第二步：克隆本仓库到你的本地电脑
   git clone https://github.com/MY2233/RhythmCassette_Player_2D
   ```

## - <u>将其作为一个音乐摆件就好</u> -

- [《律动磁带》操作说明](https://github.com/MY2233/CassettePlayer2D/blob/main/《律动磁带》操作说明.md)

## ✨ 项目预览

<table align="center">
  <tr>
    <td align="center">
      <img src="https://github.com/MY2233/CassettePlayer2D/blob/main/images/%E8%BD%AF%E4%BB%B6%E9%A6%96%E9%A1%B5.png" width="260">
      <br>
      <em>选择文件界面</em>
    </td>
    <td align="center">
      <img src="https://github.com/MY2233/CassettePlayer2D/blob/main/images/%E6%92%AD%E6%94%BE%E9%A1%B5.png" width="260">
      <br>
      <em>播放与搓碟</em>
    </td>
    <td align="center">
      <img src="https://github.com/MY2233/CassettePlayer2D/blob/main/images/%E8%BD%AF%E4%BB%B6%E8%BF%90%E8%A1%8C.png" width="260">
      <br>
      <em>交互细节</em>
    </td>
  </tr>
</table>

## 🚀 如何使用与运行

### 环境要求

- **Unity 版本:** 推荐使用 `Unity 2022.3.57f1c2` 或更高版本。
- **渲染管线:** 项目基于 `Universal Render Pipeline (URP)` 创建。

### 引用插件

- Standalone File Browser (PC端):

   用于在 Windows, Mac, Linux 上打开文件选择窗口。

  - [点击这里从 GitHub 下载插件](https://github.com/gkngkc/UnityStandaloneFileBrowser)

- Native File Picker (安卓端):

   用于在安卓设备上调用原生文件选择器。

  - [点击这里从 GitHub 下载插件](https://github.com/yasirkula/UnityNativeFilePicker)
  
- UniWindowController (PC端):
  用于在Windows，Mac，Linux上实现无边框效果。

  - [点击这里从 GitHub 下载插件](https://github.com/kirurobo/UniWindowController)

## 核心功能

- **跨平台文件选择：** 支持在 PC 和 Android 设备上选择本地的音频和图片文件。
- 动态内容加载：
  - 运行时加载用户选择的音频文件 (`.mp3`, `.wav`, `.ogg`)。
  - 运行时加载用户选择的图片 (`.png`, `.jpg`)，并将其应用为磁带的自定义壁纸。
  - 自动提取并显示歌曲名称。
- 物理交互式播放控制：
  - 通过拖拽虚拟**唱臂 (Tonearm)** 来控制音乐的播放与暂停，极具仪式感。
- 实时音频可视化 (Audio Visualizer)：
  - 唱盘上的律动方块阵列会根据音乐的频谱数据实时跳动。
  - 方块颜色、灵敏度、平滑度等均可通过脚本参数调整。
- 双模式搓碟系统 (Scratching)：
  - **唱盘搓碟：** 直接在唱盘上拖拽可实现高精度的“搓碟”效果，音高会随拖拽速度实时变化。
  - **进度条搓碟：** 拖动进度条滑块同样能实现刮擦音效。
- 多状态播放器内核：
  - 通过稳健的状态机管理，完美处理了正常播放、滑块拖动、唱盘搓碟等多种模式之间的切换，避免逻辑冲突。
  - 支持“静默寻址”，即唱臂抬起时仍可调整进度，放下后从新位置播放。
- 丰富的视觉细节：
  - 磁带轮会随音乐播放而旋转。
  - 唱盘在播放时会缓慢自转。
  - 通过自定义 Shader 为部分 UI（如音量旋钮）添加了不受旋转影响的全局光照和高光效果。
- 双场景架构：
  - **SetupScene：** 清晰的文件选择入口。
  - **PlayerScene：** 核心交互与播放界面。

## 🤖 AI 协助说明

本项目在开发过程，尤其在**脚本编写、复杂逻辑实现（如搓碟算法）、问题调试和方案设计**中，得到了 Google AI 助手 **Gemini** 的大量协助。

## 📄 许可证 (License)

本项目采用 **MIT 许可证**。

这意味着你可以自由地使用、复制、修改、合并、出版、分发、再授权及贩售本软件的副本。你只需要在你的项目中包含原始的版权和许可声明即可。