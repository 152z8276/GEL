# GEL · 1v1 手势足球原型 — 策划案网页

## 打开

直接双击 `index.html` 即可。页面不依赖框架、不需要联网。

如果浏览器限制 `file://` 下的下载或字体，建议在项目根目录启动一个本地静态服务器：

```powershell
cd C:\Users\pc\Desktop\作品集\GEL
python -m http.server 8080
```

然后打开：

```text
http://127.0.0.1:8080/Frontend/
```

## 文件

- `index.html` — 页面结构
- `styles.css` — 3D 科技运动风格、响应式布局
- `app.js` — 导航、筛选、滚动 reveal、Canvas 背景
- `assets/hero-game.jpg` — 从 Unity Game 视图截取的项目画面
- `../Docs/GEL_项目总结报告.docx` — 项目总结
- `../Docs/GEL_1v1手势足球原型_策划案.docx` — 策划案文档版

## 页面结构

1. 概览 / Hero
2. 已实现
3. 核心输入与动作判定
4. 模块拆解：数值、花式、动作、特效、音效、UI、AI、移动端
5. 计划但未做
6. 路线图
7. 下载

`已实现 / 原型 / 计划` 三个状态在页面中显式标注；筛选按钮可以只看某一类。
