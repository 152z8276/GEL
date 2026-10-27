from pathlib import Path
from datetime import date
import math
import os

from docx import Document
from docx.shared import Pt, Cm, Inches, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.enum.section import WD_SECTION
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image, ImageDraw, ImageFont

DOCS = Path(r"C:\Users\pc\Desktop\作品集\GEL\Docs")
DOCS.mkdir(parents=True, exist_ok=True)

FONT_CN = "微软雅黑"
FONT_CN_FILE = r"C:\Windows\Fonts\msyh.ttc"
FONT_CN_BOLD_FILE = r"C:\Windows\Fonts\msyhbd.ttc"

NAVY = RGBColor(0x0B, 0x1B, 0x2B)
CYAN = RGBColor(0x16, 0x9F, 0xD6)
TEAL = RGBColor(0x0F, 0x7A, 0x72)
MUTED = RGBColor(0x5B, 0x6B, 0x7A)
LIGHT = RGBColor(0xF3, 0xF7, 0xFA)


def set_east_asia(run, name=FONT_CN):
    run.font.name = name
    rPr = run._element.get_or_add_rPr()
    rFonts = rPr.find(qn("w:rFonts"))
    if rFonts is None:
        rFonts = OxmlElement("w:rFonts")
        rPr.append(rFonts)
    rFonts.set(qn("w:eastAsia"), name)
    rFonts.set(qn("w:ascii"), name)
    rFonts.set(qn("w:hAnsi"), name)


def shade(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:fill"), fill)
    tcPr.append(shd)


def set_cell_text(cell, text, bold=False, color=None, size=9.5, align=WD_ALIGN_PARAGRAPH.LEFT):
    cell.text = ""
    p = cell.paragraphs[0]
    p.alignment = align
    p.paragraph_format.space_after = Pt(2)
    run = p.add_run(str(text))
    run.bold = bold
    run.font.size = Pt(size)
    if color is not None:
        run.font.color.rgb = color
    set_east_asia(run)


def add_page_number(paragraph):
    run = paragraph.add_run()
    fldChar1 = OxmlElement("w:fldChar")
    fldChar1.set(qn("w:fldCharType"), "begin")
    instrText = OxmlElement("w:instrText")
    instrText.set(qn("xml:space"), "preserve")
    instrText.text = "PAGE"
    fldChar2 = OxmlElement("w:fldChar")
    fldChar2.set(qn("w:fldCharType"), "end")
    run._r.append(fldChar1)
    run._r.append(instrText)
    run._r.append(fldChar2)
    run.font.size = Pt(9)
    run.font.color.rgb = MUTED


def style_document(doc, header_text):
    section = doc.sections[0]
    section.page_width = Cm(21.0)
    section.page_height = Cm(29.7)
    section.top_margin = Cm(2.0)
    section.bottom_margin = Cm(1.9)
    section.left_margin = Cm(2.2)
    section.right_margin = Cm(2.2)
    section.header_distance = Cm(0.8)
    section.footer_distance = Cm(0.8)

    normal = doc.styles["Normal"]
    normal.font.name = FONT_CN
    normal.font.size = Pt(10.5)
    normal.paragraph_format.line_spacing = 1.35
    normal.paragraph_format.space_after = Pt(4)

    styles = {
        "Title": (26, NAVY, True),
        "Subtitle": (13, MUTED, False),
        "Heading 1": (17, NAVY, True),
        "Heading 2": (13, CYAN, True),
        "Heading 3": (11.5, TEAL, True),
    }
    for style_name, (size, color, bold) in styles.items():
        style = doc.styles[style_name]
        style.font.name = FONT_CN
        style.font.size = Pt(size)
        style.font.bold = bold
        style.font.color.rgb = color
        style.paragraph_format.space_before = Pt(12 if style_name == "Heading 1" else 8)
        style.paragraph_format.space_after = Pt(6)
        rPr = style.element.get_or_add_rPr()
        rFonts = rPr.find(qn("w:rFonts"))
        if rFonts is None:
            rFonts = OxmlElement("w:rFonts")
            rPr.append(rFonts)
        rFonts.set(qn("w:eastAsia"), FONT_CN)

    # Header and footer, with a clean rule under the header.
    section.different_first_page_header_footer = True
    header = section.header
    hp = header.paragraphs[0]
    hp.text = ""
    hr = hp.add_run(header_text)
    hr.font.size = Pt(8.5)
    hr.font.color.rgb = MUTED
    set_east_asia(hr)
    pPr = hp._p.get_or_add_pPr()
    pBdr = OxmlElement("w:pBdr")
    bottom = OxmlElement("w:bottom")
    bottom.set(qn("w:val"), "single")
    bottom.set(qn("w:sz"), "4")
    bottom.set(qn("w:space"), "4")
    bottom.set(qn("w:color"), "169FD6")
    pBdr.append(bottom)
    pPr.append(pBdr)

    footer = section.footer
    fp = footer.paragraphs[0]
    fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    fr = fp.add_run("GEL · P0 原型  |  ")
    fr.font.size = Pt(9)
    fr.font.color.rgb = MUTED
    set_east_asia(fr)
    add_page_number(fp)


def add_cover(doc, title, subtitle, image_path, tag):
    doc.add_picture(str(image_path), width=Inches(6.6))
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p.paragraph_format.space_before = Pt(18)
    r = p.add_run(title)
    r.font.size = Pt(28)
    r.bold = True
    r.font.color.rgb = NAVY
    set_east_asia(r)

    p = doc.add_paragraph()
    r = p.add_run(subtitle)
    r.font.size = Pt(13)
    r.font.color.rgb = MUTED
    set_east_asia(r)

    p = doc.add_paragraph()
    p.paragraph_format.space_before = Pt(18)
    r = p.add_run(tag)
    r.font.size = Pt(9.5)
    r.font.color.rgb = CYAN
    r.bold = True
    set_east_asia(r)

    doc.add_paragraph()
    meta = doc.add_table(rows=4, cols=2)
    meta.style = "Table Grid"
    meta.alignment = WD_TABLE_ALIGNMENT.LEFT
    facts = [
        ("项目", "《绿茵·星能联盟》"),
        ("阶段", "P0 可玩原型"),
        ("引擎", "Unity 2022.3.62f3c1  /  Built-in Render Pipeline"),
        ("版本日期", f"v1.0  ·  {date.today().isoformat()}"),
    ]
    for i, (k, v) in enumerate(facts):
        set_cell_text(meta.cell(i, 0), k, bold=True, color=NAVY, size=9.5)
        set_cell_text(meta.cell(i, 1), v, size=9.5)
        shade(meta.cell(i, 0), "EAF4FA")

    doc.add_page_break()


def add_toc(doc, items):
    doc.add_heading("目录", level=1)
    for item in items:
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(2)
        r = p.add_run(item)
        r.font.size = Pt(10)
        set_east_asia(r)
    doc.add_page_break()


def add_body(doc, text, bold=False, color=None):
    p = doc.add_paragraph()
    r = p.add_run(text)
    r.bold = bold
    if color:
        r.font.color.rgb = color
    set_east_asia(r)
    return p


def add_bullets(doc, items):
    for item in items:
        p = doc.add_paragraph(style="List Bullet")
        p.paragraph_format.space_after = Pt(2)
        r = p.add_run(item)
        set_east_asia(r)


def add_table(doc, headers, rows, sizes=None):
    table = doc.add_table(rows=1, cols=len(headers))
    table.style = "Table Grid"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = True

    # Repeat the header when a long table breaks across pages.
    headerRow = table.rows[0]
    trPr = headerRow._tr.get_or_add_trPr()
    tblHeader = OxmlElement("w:tblHeader")
    tblHeader.set(qn("w:val"), "true")
    trPr.append(tblHeader)

    for i, header in enumerate(headers):
        set_cell_text(table.cell(0, i), header, bold=True, color=RGBColor(0xFF, 0xFF, 0xFF), size=9)
        shade(table.cell(0, i), "0B1B2B")

    for row in rows:
        cells = table.add_row().cells
        for i, value in enumerate(row):
            set_cell_text(cells[i], value, size=9)
            if len(rows) % 2 == 0:
                pass

    # Alternate row shading for readability.
    for r_idx, row in enumerate(table.rows[1:], start=1):
        if r_idx % 2 == 0:
            for cell in row.cells:
                shade(cell, "F3F8FB")

    doc.add_paragraph()
    return table


def create_cover_image(path, title, subtitle):
    width, height = 1600, 900
    img = Image.new("RGB", (width, height), (7, 18, 30))
    draw = ImageDraw.Draw(img)

    # Deep blue-green gradient.
    for y in range(height):
        t = y / height
        color = (
            int(7 + 8 * t),
            int(22 + 22 * (1 - t)),
            int(36 + 30 * (1 - t)),
        )
        draw.line([(0, y), (width, y)], fill=color)

    # Soft tech rings.
    for r in range(260, 620, 70):
        alpha = max(0, 70 - (r - 260) // 8)
        draw.ellipse(
            (width - r - 120, -r // 2 + 80, width + r - 120, r // 2 + 80),
            outline=(22, 160, 210, alpha),
            width=2,
        )

    # Circuit-like lines.
    for i in range(18):
        y0 = 90 + i * 43
        points = []
        x = 40
        while x < width - 30:
            points.append((x, y0 + int(18 * math.sin(x * 0.021 + i))))
            x += 90
        draw.line(points, fill=(17, 94, 112), width=1)

    # Accent block and glow line.
    draw.rectangle((78, 660, 760, 664), fill=(25, 190, 230))
    draw.rectangle((78, 116, 86, 420), fill=(16, 140, 155))

    small_font = ImageFont.truetype(FONT_CN_FILE, 23)
    label_font = ImageFont.truetype(FONT_CN_BOLD_FILE, 28)

    # The Word cover carries the title; the image is only the visual banner.
    draw.text((82, 715), "GEL  ·  P0 PLAYABLE PROTOTYPE", font=small_font, fill=(112, 180, 200))
    draw.text((82, 755), "UNITY 2022.3  /  MOBILE 1v1 FOOTBALL", font=small_font, fill=(88, 152, 170))
    draw.text((82, 92), "GEL / P0", font=label_font, fill=(31, 184, 209))

    img.save(path, quality=95)


def build_summary():
    doc = Document()
    style_document(doc, "GEL · 项目总结报告")

    cover = DOCS / "cover_summary.png"
    create_cover_image(cover, "绿茵·星能联盟", "P0 可玩原型 · 项目总结报告")
    add_cover(
        doc,
        "《绿茵·星能联盟》",
        "P0 可玩原型 · 项目总结报告",
        cover,
        "PROJECT SUMMARY  /  2026.10",
    )

    add_toc(doc, [
        "摘要",
        "1. 项目概况",
        "2. 最终可玩内容",
        "3. 技术架构",
        "4. 核心玩法系统",
        "5. 场景与视觉",
        "6. 移动端输入",
        "7. 工程与版本管理",
        "8. 验证状态",
        "9. 已知限制与风险",
        "10. 关键决策与经验",
        "11. 下一步建议",
        "附录 A：操作说明",
        "附录 B：项目文件",
        "附录 C：检查点清单",
        "附录 D：关键参数",
    ])

    doc.add_heading("摘要", level=1)
    add_body(doc, "《绿茵·星能联盟》是一个面向移动端的 1v1 足球手感原型。项目以越肩视角、手势射门、子弹时间和花式过人为核心体验，在 Unity 2022.3 中使用程序化场景与免费 Humanoid 模型快速完成了可玩闭环。")
    add_body(doc, "项目完成了从射门、带球、抢断、AI 防守、相机、慢动作到移动端摇杆/触屏输入的完整链路，并将视觉方向收敛为“明亮草坪 + 半透明荧光蓝科技穹顶 + 电路纹理 + 三段式角柱”的超智能足球风格。")
    add_body(doc, "当前版本是 P0 原型：功能链路完整，但动画、音频、正式 UI、Android 真机适配和部分手感参数仍属于验证/占位状态。本文档既记录最终成果，也明确未完成项，便于后续接手。")


    doc.add_heading("1. 项目概况", level=1)
    add_table(doc, ["项目项", "内容"], [
        ["项目名称", "《绿茵·星能联盟》"],
        ["项目类型", "移动端 1v1 足球手感 Demo"],
        ["核心关键词", "越肩视角、手势射门、子弹时间、牛尾巴、科技球场"],
        ["引擎", "Unity 2022.3.62f3c1"],
        ["渲染", "Built-in Render Pipeline"],
        ["输入", "旧 Input Manager；桌面键鼠 + Android 触屏/摇杆"],
        ["比赛规则", "先进 3 球或 120 秒内比分领先者获胜"],
        ["当前阶段", "P0 可玩原型 / 功能闭环完成"],
        ["工程路径", r"C:\Users\pc\Desktop\作品集\GEL"],
    ])
    add_body(doc, "本项目的目标不是做完整商业版本，而是验证三个问题：")
    add_bullets(doc, [
        "越肩镜头 + 手势射门是否比传统按钮射门更有身体感；",
        "子弹时间和镜头拉近是否能强化射门/花式的演出；",
        "在手机触屏上，摇杆、射门手势和花式手势能否形成稳定的双通道操作。",
    ])

    doc.add_heading("2. 最终可玩内容", level=1)
    add_body(doc, "当前版本包含一条完整的比赛链路：")
    add_bullets(doc, [
        "玩家与 AI 在封闭科技球场内进行 1v1；",
        "WASD/左摇杆移动，A/D 最多 270° 缓慢转向，S 根据对手位置选择后撤或 180° 转身；",
        "从球命中圈内起手拖动：射门；手势轨迹拟合成二次曲线，支持弧线、射程和垂直修正；",
        "从球外起手：直线/近似直线为爆发，非直线为牛尾巴 Elastico；",
        "射门进入子弹时间、镜头拉近；Elastico 也自动进入子弹时间并拉近；",
        "抢断、僵直、球权硬锁、AI 防守和 AI 射门形成基础攻防闭环；",
        "移动端左下虚拟摇杆、右侧触摸手势、右上 TILT/CAL 按钮。",
    ])

    doc.add_heading("3. 技术架构", level=1)
    add_table(doc, ["模块", "主要文件", "职责"], [
        ["场景生成", "Assets/Editor/P0SceneBuilder.cs", "一键生成球场、球门、穹顶、角柱、球员、球、相机、UI 和控制器"],
        ["输入", "GestureInput.cs / MobileControls.cs / TiltInput.cs", "鼠标/触摸手势、摇杆、设备倾斜、射门与花式识别入口"],
        ["球员", "PlayerAgent.cs / PlayerMotor.cs / PlayerVisual.cs", "身份、移动、射门、爆发、重心倾斜、程序化腿部动作"],
        ["重心", "WeightSystem.cs", "Balanced / Lean / Commit / Recover，重心偏移和假动作收益"],
        ["球", "BallSimulator.cs / PossessionManager.cs", "自定义球物理、曲线射门、球权、带球跟随、硬锁"],
        ["抢断", "TackleSystem.cs", "抢断动作、最近脚距离、成功率、僵直与冷却"],
        ["相机", "ShoulderCamera.cs", "越肩跟随、子弹时间拉近、FOV、位置/旋转阻尼"],
        ["慢动作", "SlowMotionController.cs", "手动/近身/射门/花式触发，强制进入与退出"],
        ["AI", "AIController.cs", "追球、站位、铲球、射门、延迟读取重心"],
        ["比赛", "MatchDirector.cs / GoalTrigger.cs", "比分、计时、开球、进球、重开"],
        ["HUD", "PrototypeHUD.cs", "临时比分、计时、球权、手势结果"],
    ])
    add_body(doc, "整体架构保持“单个 Prototype 场景 + 运行时组件”的方式，优先验证手感而不是做工程分层。正式版本需要把 UI、输入、比赛状态和配置拆成独立模块。")

    doc.add_heading("4. 核心玩法系统", level=1)
    doc.add_heading("4.1 射门", level=2)
    add_bullets(doc, [
        "起点在球命中圈内时，意图锁定为 Shot，不允许被花式改写；",
        "手势屏幕点用于拟合弧度，蓝线投影只用于显示，不参与球的世界轨迹；",
        "射程限制 8–30m，弧线有曲率/横向偏移上限；",
        "球使用二次贝塞尔 LaunchCurve，射门前有 0.35s 前摇，球缓慢前移；",
        "松手后退出子弹时间，球以前摇结束帧真正飞出。",
    ])
    add_body(doc, "最终调参：CurveGain=1.0，ShotArc=1.0，MaxCurvature=0.22，MaxCurveOffset=1.8m。")

    doc.add_heading("4.2 爆发与牛尾巴", level=2)
    add_bullets(doc, [
        "球外起手进入 DribbleAim；直线/近似直线 → Simple 爆发；有明显弯折 → Elastico；",
        "二元判定：最大偏离/路径长度 > 5% 或 路径长度/弦长 > 1.10，判为花式；",
        "花式突进方向取手势最后一段的切线方向，而不是起点到终点的净位移；",
        "commit 同时触发短时高速位移、程序化伸腿、子弹时间和镜头拉近；",
        "爆发距离约 2.4–2.7m，明显快于 7.5m/s 的奔跑；",
        "球权在爆发期间硬锁，避免瞬间丢球。",
    ])

    doc.add_heading("4.3 重心与倾斜", level=2)
    add_bullets(doc, [
        "WeightSystem 维护局部二维重心、Lean/Commit/Recover 状态；",
        "手势前段写入重心倾斜，AI 延迟读取并向该方向偏移；",
        "设备倾斜为可选输入，默认关闭，加速度计 + 低通 + 2.5° 死区 + 22° 上限；",
        "倾斜只改变重心，不改变动作类型和最终方向；",
        "PlayerVisual 对上半身做程序化倾斜，root 和镜头不跟随。"
    ])

    doc.add_heading("4.4 抢断与攻防", level=2)
    add_bullets(doc, [
        "Q/右键或 AI 自动抢断；动作持续 0.35s，按最近脚到球距离结算；",
        "成功率 = TackleAbility × BallControlAbility × closeness × 随机扰动，限制 5%–90%；",
        "成功：球权转移、被抢者 0.2s 僵直、2s 抢断锁；失败：抢断者 0.15s 僵直；",
        "AI 有 0.18s 反应延迟、较低加速度和转向速度。",
    ])

    doc.add_heading("5. 场景与视觉", level=1)
    add_body(doc, "球场回到 42×26m 的比赛比例，球门线 x=±18、边线 z=±12；外围是约 44×30m 的封闭活动范围。视觉上取消可见围墙，由半胶囊穹顶直接覆盖整座球场。")
    add_bullets(doc, [
        "穹顶：半圆柱 + 两端半椭圆封头，从地面起拱，最高约 12m；",
        "穹顶主体：半透明蓝色、不发光；外层深色电路纹路做呼吸明暗；",
        "四角：三角形棱台基座 + 悬浮发光能量核心 + 悬浮六棱柱；",
        "草坪：程序生成纹理、割草条纹、中圈/禁区/球门区/点球点/角球弧；",
        "物理：边界靠近似真实参数，边线/底线也有独立碰撞层。",
    ])
    add_table(doc, ["物理项", "参数"], [
        ["边界弹性", "bounciness 0.66 / friction 0.12"],
        ["草皮", "bounciness 0.35 / friction 0.40"],
        ["球", "bounciness 0.35 / drag 0.025 / angularDrag 0.18"],
        ["穹顶/顶棚", "不可见碰撞体，高约 12.2m"],
    ])

    doc.add_heading("6. 移动端输入", level=1)
    add_bullets(doc, [
        "MobileControls 生成左下虚拟摇杆，输出到 PlayerMotor.VirtualInput；",
        "GestureInput 支持 Input.touches，摇杆手指和手势手指可同时存在；",
        "摇杆区域、TILT/CAL 按钮区域不会被手势误触；",
        "触屏命中圈与最小滑动距离按 Screen.height 缩放；",
        "TiltInput 默认关闭，手机右上提供 TILT/CAL 按钮；桌面 Y/U 调试。",
    ])
    add_body(doc, "Android 构建脚本位于 Assets/Editor/AndroidTestBuilder.cs，项目默认横屏 Landscape Left；当前环境仍缺 Unity Android SDK/NDK，未产出最终安装包。")

    doc.add_heading("7. 工程与版本管理", level=1)
    add_body(doc, "工程已初始化 Git，并为每个阶段建立标签。出现 bug 或效果不满意时，可以直接 checkout 对应标签回退。")
    add_table(doc, ["标签", "内容"], [
        ["checkpoint-00-p0-mobile-input", "移动端摇杆/触摸手势/球场物理基线"],
        ["checkpoint-01-weight-core", "重心核心、身体 Lean、AI 读取、假动作加成"],
        ["checkpoint-02-device-tilt", "设备倾斜与校准、手机 TILT/CAL"],
        ["checkpoint-03-gesture-recognition", "规则式手势识别"],
        ["checkpoint-04-dribble-animations", "过人手势到动作状态映射"],
        ["checkpoint-05-mobile-build-prep", "横屏、Android 构建脚本、真机清单"],
        ["checkpoint-06-elastico", "牛尾巴手势与程序化腿动作"],
        ["checkpoint-07 至 13", "Elastico 起点/三点/方向/子弹时间迭代"],
        ["checkpoint-14-ball-start-shot", "球上起手严格射门"],
        ["checkpoint-15/16", "转角识别迭代"],
        ["checkpoint-17", "直线/非直线二元分类"],
        ["checkpoint-18-tangent-flair", "最终切线方向与二元分类"],
    ])

    doc.add_heading("8. 验证状态", level=1)
    add_table(doc, ["验证项", "状态", "说明"], [
        ["C# 编译", "已通过", "Assembly-CSharp 与 Editor 程序集均 0 error"],
        ["编辑器运行 smoke test", "已做", "打开场景/Play 无运行时异常"],
        ["射门链路", "已验证基础链路", "手势、前摇、曲线、子弹时间均已接通"],
        ["爆发/花式", "逻辑已接通，识别待真机手感确认", "二元分类、切线方向、程序化腿已实现"],
        ["移动端摇杆/触屏", "代码已接通", "未完成最终 Android 真机回归"],
        ["Android APK", "未产出", "缺 SDK/NDK 与最终真机流程"],
    ])

    doc.add_heading("9. 已知限制与风险", level=1)
    add_bullets(doc, [
        "动画仍是免费素材占位；牛尾巴使用程序化骨骼，不是最终动作资产；",
        "HUD 是 OnGUI 原型，未做正式 UGUI/分辨率和刘海适配；",
        "花式识别受触摸采样率影响；快速手势可能只有 2–3 个采样点；",
        "设备倾斜默认关闭，横屏左右方向可能需要 InvertX/InvertY；",
        "没有音频、特效、正式过场、暂停/设置/结算页面；",
        "没有出界规则，边界是封闭回弹；没有门将、犯规、越位；",
        "AI 仍是单层状态机，不是行为树；",
        "没有最终 Android 包与性能数据；",
        "免费素材的授权需要在正式发行前再核对。",
    ])

    doc.add_heading("10. 关键决策与经验", level=1)
    add_bullets(doc, [
        "先用手势的“起点区域”区分动作类型，再用轨迹形状区分爆发/花式；不要用多个手势分类器互相抢输入；",
        "射门/花式/爆发必须有明确的输入锁定和退出条件，否则方向键、镜头和动画会互相叠加；",
        "动作速度不能只提高数值；子弹时间下 0.25× 会让爆发看起来变慢，必须强制退出或使用实时位移；",
        "镜头旋转和身体 root 旋转要分开；花式动作只旋转骨骼/可视层，不能让相机突然转；",
        "每个阶段建立 Git 标签，比“最后再整理”更适合手感型原型。",
    ])

    doc.add_heading("11. 下一步建议", level=1)
    add_bullets(doc, [
        "P0.5：完成 Android SDK/NDK 安装，产出真机 APK，验证摇杆、触摸、倾斜和帧率；",
        "P1：将 HUD 从 OnGUI 迁移到 UGUI/Canvas，加入安全区、横屏适配和按键重映射；",
        "P1：用正式动作替换占位素材，至少补齐牛尾巴、爆发、假动作、射门四类动作；",
        "P1：加入音效、观众环境声、球鞋/触球反馈和子弹时间音效；",
        "P2：AI 改为行为树/效用 AI，加入门将、体力、比赛模式；",
        "P2：把重心/倾斜/花式识别参数提取为可配置资产，做数据化调参。",
    ])

    doc.add_heading("附录 A：操作说明", level=1)
    add_table(doc, ["输入", "行为"], [
        ["WASD / 方向键", "移动与转向；A/D 单次最多 270°"],
        ["左下摇杆 / 触屏", "左右转向，上下前进/后退"],
        ["球命中圈内拖动", "射门"],
        ["球命中圈外拖动", "直线=爆发，弯折=Elastico"],
        ["Q / 鼠标右键", "抢断"],
        ["Space", "手动子弹时间"],
        ["T", "AI 自动行为开关"],
        ["R", "重开一局"],
        ["Y / U（桌面）", "切换倾斜 / 重新校准"],
    ])

    doc.add_heading("附录 B：项目文件", level=1)
    add_table(doc, ["路径", "说明"], [
        ["Assets/Scenes/P0_Prototype.unity", "主场景"],
        ["Assets/Editor/P0SceneBuilder.cs", "一键场景生成"],
        ["Assets/Scripts/Input/", "GestureInput、MobileControls、TiltInput、DribbleGestureRecognizer"],
        ["Assets/Scripts/Player/", "PlayerAgent、PlayerMotor、PlayerVisual、WeightSystem、PlayerAnimationDriver"],
        ["Assets/Scripts/Core/", "MatchDirector、PossessionManager、TackleSystem、SlowMotionController、StadiumGlow"],
        ["Assets/Scripts/Camera/ShoulderCamera.cs", "越肩/子弹时间相机"],
        ["Assets/Animations/FreePlayer.controller", "玩家动作状态机"],
        ["Docs/", "本报告与前端策划案"],
    ])

    doc.add_heading("附录 C：检查点清单", level=1)
    add_body(doc, "最新检查点为 checkpoint-18-tangent-flair；Git 历史从 checkpoint-00 到 checkpoint-18 完整保留。")
    add_bullets(doc, [
        "回退到某阶段：git checkout <checkpoint-tag>",
        "回到主线：git checkout main",
        "只恢复单个文件：git restore --source <checkpoint-tag> <path>",
    ])

    doc.add_heading("附录 D：关键参数", level=1)
    add_table(doc, ["参数", "值", "说明"], [
        ["MoveSpeed", "7.5 m/s", "无球速度"],
        ["DribbleSpeed", "6 m/s", "带球速度"],
        ["DribbleBurstPeakSpeed", "16 m/s", "带球爆发峰值"],
        ["DefensiveBurstPeakSpeed", "18 m/s", "防守爆发峰值"],
        ["DribbleInstantOffset", "0.5 m", "commit 即时位移"],
        ["DribbleBurstDuration", "0.12 s", "爆发持续时间"],
        ["CurveGain", "1.0", "射门弧度增益"],
        ["MaxCurveOffset", "1.8 m", "横向偏移上限"],
        ["MaxCurvature", "0.22", "曲率上限"],
        ["DuelDistance", "3 m", "自动慢动作距离"],
        ["ElasticoPoseDuration", "0.34 s", "程序化伸腿时间"],
    ])

    out = DOCS / "GEL_项目总结报告.docx"
    doc.save(out)
    return out


def build_frontend_plan():
    doc = Document()
    style_document(doc, "GEL · 完整前端策划案")

    cover = DOCS / "cover_frontend.png"
    create_cover_image(cover, "绿茵·星能联盟", "完整前端策划案 · 客户端与玩家表现层")
    add_cover(
        doc,
        "《绿茵·星能联盟》",
        "完整前端策划案 · 客户端与玩家表现层",
        cover,
        "FRONTEND / UX / PRESENTATION  ·  2026.10",
    )

    add_toc(doc, [
        "0. 文档说明",
        "1. 产品定位",
        "2. 目标用户与使用场景",
        "3. 设计支柱",
        "4. 核心循环",
        "5. 比赛流程与页面状态机",
        "6. 页面清单与线框",
        "7. 输入与手势系统",
        "8. 镜头与空间表现",
        "9. HUD 与反馈",
        "10. 射门体验",
        "11. 过人、花式与重心",
        "12. 防守与 AI 表现",
        "13. 新手教学",
        "14. 视觉风格",
        "15. 音频与震动",
        "16. 移动端适配与性能",
        "17. 客户端技术架构",
        "18. 数据与埋点",
        "19. 可访问性与舒适度",
        "20. 验收标准",
        "21. 路线图与风险",
        "附录：UI 文案与参数",
    ])

    doc.add_heading("0. 文档说明", level=1)
    add_body(doc, "本文中的“前端”指玩家直接接触的客户端表现层：页面流程、HUD、输入、镜头、反馈、视觉和移动端适配，不包含服务器、匹配、账号、支付等后端能力。")
    add_body(doc, "适用阶段：P0 原型验证通过之后，进入 P1 可展示 Demo / 可测试版本。当前 P0 已具备功能链路，本文档描述的是把它整理成完整体验所需的玩家侧方案。")

    doc.add_heading("1. 产品定位", level=1)
    add_table(doc, ["项目", "定义"], [
        ["一句话", "一款强调手势射门和子弹时间演出的 1v1 动漫足球手游"],
        ["平台", "Android 优先，后续 iOS"],
        ["单局时长", "90–150 秒"],
        ["操作", "左摇杆移动 + 右手手势射门/过人"],
        ["视觉", "明亮草坪、超智能足球式科技穹顶、荧光蓝 UI"],
        ["情绪", "短促、爽快、有身体感、有花式表演空间"],
        ["商业目标", "Demo/作品集验证，不追求完整商业化"],
    ])
    add_body(doc, "差异化不在“足球规则多”，而在于把射门和过人做成可被身体感知的手势动作，并用子弹时间把关键一瞬间拉长。")

    doc.add_heading("2. 目标用户与使用场景", level=1)
    add_bullets(doc, [
        "核心用户：喜欢足球、动漫、短局竞技手游的玩家；",
        "次要用户：动作游戏玩家、直播/短视频观众、作品集评审；",
        "使用场景：通勤、课间、睡前 2–5 分钟；单手/双手横屏；",
        "设备预期：中端 Android，60fps 为目标，30fps 可接受；",
        "网络预期：P0 为本地单机；正式版可先做异步/单机，再考虑实时联机。",
    ])

    doc.add_page_break()
    doc.add_heading("3. 设计支柱", level=1)
    add_table(doc, ["支柱", "表现", "设计约束"], [
        ["身体感", "手势划出射门弧线，人物伸腿、拨球、爆发", "输入反馈必须在 100ms 内出现"],
        ["一瞬间的演出", "射门/牛尾巴进入子弹时间、镜头拉近", "演出不能遮挡球门和防守者"],
        ["易读", "直线=爆发，弯折=花式，球上=射门", "动作类型不靠隐藏规则"],
        ["短局", "3 球/120 秒，快速重开", "任何页面不超过 2 次点击回到比赛"],
        ["可回退", "每个阶段有检查点", "手感和美术迭代可快速回滚"],
    ])

    doc.add_heading("4. 核心循环", level=1)
    add_body(doc, "单局循环：")
    add_bullets(doc, [
        "进入比赛 → 开球 → 争球/带球 → 选择射门或花式 → 进球/被抢断 → 重新开球；",
        "比分或时间结束 → 结算 → 再来一局 / 返回主界面；",
        "每局目标：用最少的动作完成一次有表演性的进球。",
    ])
    add_body(doc, "长线循环（P1 以后）：完成比赛 → 获得角色/球场/动作外观 → 解锁更高难度 AI 或新模式。")

    doc.add_heading("5. 比赛流程与页面状态机", level=1)
    add_table(doc, ["状态", "进入条件", "退出条件", "玩家可见内容"], [
        ["Boot", "启动", "资源加载完成", "Logo / 加载进度"],
        ["MainMenu", "Boot 完成", "点击开始", "开始比赛、设置、操作说明"],
        ["MatchIntro", "开始比赛", "1.5s 介绍结束", "对阵、球场、开球提示"],
        ["Kickoff", "开球", "球被触碰/时间到", "中圈、倒计时、球权提示"],
        ["Playing", "开球完成", "进球/时间到", "完整 HUD 与操作"],
        ["Goal", "球进门", "进球演出结束", "比分变化、慢动作、庆祝"],
        ["Paused", "暂停", "继续/退出", "半透明遮罩、设置"],
        ["Result", "3 球/时间到", "再来一局/返回", "比分、数据、重开"],
    ])
    add_body(doc, "P0 可以保留 MatchDirector 直接开球，但正式前端必须把状态机独立出来，避免 UI、比分和输入互相调用。")

    doc.add_heading("6. 页面清单与线框", level=1)
    doc.add_heading("6.1 主界面", level=2)
    add_body(doc, "结构：背景为球场远景/穹顶，中央为“开始比赛”，下方为“操作说明 / 设置”，右上为版本号和账号入口。")
    add_table(doc, ["区域", "内容"], [
        ["顶部", "Logo、版本号、设置"],
        ["中央", "开始比赛（主按钮）、继续训练"],
        ["下方", "操作说明、音量、语言"],
        ["背景", "缓慢环绕的科技球场，不抢文字"],
    ])

    doc.add_heading("6.2 比赛 HUD", level=2)
    add_body(doc, "比赛 HUD 必须保持低遮挡，核心信息在顶部，操作反馈在手指附近。")
    add_table(doc, ["区域", "内容", "说明"], [
        ["左上", "比分 / 球权", "常驻，数字大"],
        ["顶部中央", "倒计时", "最后 10 秒变红并脉冲"],
        ["右上", "暂停 / 设置", "小按钮，避免误触"],
        ["左下", "虚拟摇杆", "半透明，安全区内"],
        ["右侧", "手势轨迹", "蓝=射门，黄=爆发/花式"],
        ["球附近", "命中圈", "淡绿色，仅持球时显示"],
        ["屏幕中央下方", "结果提示", "Elastico / Beat / Shot 等"],
    ])

    doc.add_heading("6.3 结算页", level=2)
    add_bullets(doc, [
        "比分与胜负文案；",
        "本局数据：射门次数、射正、花式成功、抢断；",
        "再来一局 / 返回主界面；",
        "首次胜利解锁提示（P1）。",
    ])

    doc.add_heading("7. 输入与手势系统", level=1)
    add_body(doc, "输入采用“左摇杆 + 右手手势”的双通道。动作类型在按下瞬间由起点区域决定，后续不再改判。")
    add_table(doc, ["输入", "判定", "结果"], [
        ["球命中圈内按下拖动", "起点在球上", "Shot"],
        ["球命中圈外按下拖动", "起点在球外", "DribbleAim"],
        ["路径近似直线", "最大偏离 ≤ 路径 5%，路径/弦 ≤ 1.10", "Simple 爆发"],
        ["路径有明显弯折", "超出上述阈值", "Elastico"],
        ["花式方向", "最后一段切线", "向斜前方爆发"],
    ])
    add_body(doc, "输入状态机：")
    add_table(doc, ["阶段", "行为", "失败回退"], [
        ["PointerDown", "记录起点、判断球命中圈、锁定意图", "UI 区域忽略"],
        ["Drag", "采样屏幕点、画轨迹、更新重心", "点数不足时仍可完成 Tap"],
        ["Release", "识别直线/弯折、提交方向、触发动作", "识别失败回退 Simple"],
        ["Lock", "爆发/花式期间锁移动输入", "结束后恢复"],
    ])
    add_bullets(doc, [
        "球上起手绝不进入花式；这是硬规则；",
        "所有采样点使用原始 Input 屏幕坐标，不做地面 raycast；",
        "轨迹显示可以使用相机平面，但判定与方向不用投影结果；",
        "快速手势只有 3 个点也必须可识别。",
    ])

    doc.add_heading("8. 镜头与空间表现", level=1)
    add_bullets(doc, [
        "常规：越肩、略偏右、玩家在画面左侧，保留前方球门和对手；",
        "子弹时间：镜头拉近、FOV 降低、高度接近球心并上仰；",
        "花式：自动进入子弹时间，但不让 root/相机跟随手势旋转；",
        "镜头阻尼要足够，避免高速爆发时抖动；",
        "手机晕动症选项：降低镜头拉近幅度、关闭动态FOV。",
    ])

    doc.add_heading("9. HUD 与反馈", level=1)
    add_table(doc, ["反馈", "表现", "时长"], [
        ["射门开始", "轨迹变蓝、球圈收束、轻微时间减速", "0.15s 进入"],
        ["爆发", "黄色短线、地面尘土、角色 Lean", "0.2s"],
        ["花式识别", "黄色折线、Elastico 文本、镜头拉近", "0.3–0.6s"],
        ["成功过人", "Beat! + 防守者僵直 + 轻微屏幕震动", "0.28s"],
        ["抢断成功", "红色冲击、球权图标切换", "0.2s"],
        ["进球", "慢动作、网抖动、比分变化", "1.2–1.8s"],
    ])
    add_body(doc, "HUD 文案要短：`Elastico`、`Beat!`、`Shot`、`Burst`，避免长句遮挡比赛。")

    doc.add_heading("10. 射门体验", level=1)
    add_bullets(doc, [
        "射门起点必须在球上，命中圈要足够大，手机上建议 90–110px 或屏高 7%；",
        "手势终点决定方向/射程，屏幕曲线决定弧度；",
        "弧线要有上限，避免一脚从边线绕回球门；",
        "射门前摇 0.35s：角色有起脚动作，球缓慢前移；",
        "松手后立即退出子弹时间，让球在正常速度飞行；",
        "进球/偏出都要有明确反馈，不能静默。",
    ])

    doc.add_heading("11. 过人、花式与重心", level=1)
    add_bullets(doc, [
        "直线/近似直线：短促爆发，用于摆脱或补位；",
        "弯折：牛尾巴 Elastico，伸腿 + 斜前方突进；",
        "重心：手势前段让角色 Lean，防守 AI 延迟读取并偏移；",
        "倾斜：可选辅助，不改变动作类型；",
        "所有花式动作必须和爆发一样快，不能出现慢动作里慢走；",
        "花式成功：防守者僵直、镜头拉近、HUD 显示 `Beat [Elastico]`。",
    ])
    add_body(doc, "动作节奏建议：识别 0.1s 内、爆发 0.12s、程序化腿 0.34s、镜头演出 0.6s。")

    doc.add_heading("12. 防守与 AI 表现", level=1)
    add_bullets(doc, [
        "防守 AI 需要有反应延迟，不能瞬间读手势；",
        "AI 抢断前有起脚动作和音效，让玩家能读；",
        "被过掉后短僵直，但保留反抢窗口；",
        "防守爆发与进攻爆发共用冷却，形成攻防节奏；",
        "AI 难度用反应时间、加速度、抢断能力区分，不用作弊读输入。",
    ])

    doc.add_heading("13. 新手教学", level=1)
    add_table(doc, ["步骤", "教学", "通过条件"], [
        ["1", "左摇杆移动，接近球", "碰到球"],
        ["2", "从球上拖向球门", "完成一次射门"],
        ["3", "从球外直线滑", "完成一次爆发"],
        ["4", "从球外画 > 或 <", "完成一次 Elastico"],
        ["5", "进入 1v1 比赛", "完成 30 秒比赛"],
    ])
    add_body(doc, "教学必须允许跳过；每一步只教一个动作，不要一次展示所有手势。")

    doc.add_heading("14. 视觉风格", level=1)
    add_bullets(doc, [
        "整体：明亮动漫足球 + 超智能足球式科技球场；",
        "草坪：高饱和绿色、割草条纹，避免真实照片纹理；",
        "穹顶：半透明蓝、深色电路、呼吸灯，不用粗网格/铁丝网；",
        "UI：深蓝底 + 青色高光，少量橙色警示；",
        "球员：高对比队服（玩家蓝、AI红），轮廓清楚；",
        "字体：中文使用清晰无衬线，数字用等宽/窄体增强计时感。",
    ])

    doc.add_heading("15. 音频与震动", level=1)
    add_table(doc, ["事件", "音频", "震动"], [
        ["触球", "短促皮革声", "轻"],
        ["射门", "踢球 + 低频冲击", "中"],
        ["子弹时间", "低频 whoosh + 心跳", "无"],
        ["花式成功", "观众惊呼 + 动作声", "中"],
        ["抢断", "鞋钉/碰撞", "中"],
        ["进球", "观众爆发 + 哨声", "长"],
    ])
    add_body(doc, "P0 没有音频；正式版音频优先级应高于新增玩法，因为触球/射门反馈直接影响手感。")

    doc.add_heading("16. 移动端适配与性能", level=1)
    add_bullets(doc, [
        "横屏 Landscape Left；使用 Screen.safeArea 避开刘海/圆角；",
        "摇杆和按钮使用屏幕比例，不写死像素；",
        "触屏点采样按帧率自适应，最低 30fps 仍要能识别三点折线；",
        "目标 60fps：半透明穹顶、电路纹理、粒子要控制 overdraw；",
        "透明材质不要多层叠加；穹顶/球网/UI 做分层；",
        "提供低画质：关闭穹顶电路动态、减少粒子、降低分辨率。",
    ])
    add_table(doc, ["预算", "目标"], [
        ["Draw Calls", "< 120"],
        ["三角面", "< 120k"],
        ["纹理内存", "< 180MB"],
        ["CPU 主线程", "< 8ms"],
        ["发热", "10 分钟不明显降频"],
    ])

    doc.add_heading("17. 客户端技术架构", level=1)
    add_table(doc, ["层", "推荐实现"], [
        ["App Flow", "Boot → Menu → Match → Result 状态机"],
        ["UI", "UGUI Canvas + CanvasScaler 1920×1080，安全区适配"],
        ["Input", "InputRouter：鼠标/触摸统一指针；GestureInput 只做手势"],
        ["Gameplay", "PlayerMotor / Ball / Possession / Tackle 独立组件"],
        ["Presentation", "Animation、VFX、Audio、Camera 分层，不直接改玩法数据"],
        ["Config", "ScriptableObject 配置射门/爆发/识别阈值"],
        ["Telemetry", "事件队列 + 本地日志，失败不阻塞 gameplay"],
    ])
    add_body(doc, "P0 的 OnGUI 只适合原型；正式版应迁移到 UGUI/UI Toolkit，并把 HUD 与输入区域绑定，避免手势与按钮误触。")

    doc.add_heading("18. 数据与埋点", level=1)
    add_table(doc, ["事件", "字段", "用途"], [
        ["match_start", "mode, ai_difficulty", "对局漏斗"],
        ["shot", "curve, distance, result", "射门手感"],
        ["dribble", "style, success, duration", "爆发/花式使用率"],
        ["elastico", "recognized, success, path_points", "花式识别质量"],
        ["tackle", "result, distance", "攻防平衡"],
        ["goal", "time, score", "节奏"],
        ["quit", "phase, time", "流失点"],
    ])
    add_body(doc, "首要不记录个人身份信息；Demo 阶段可只写本地日志，正式版再做合规上报。")

    doc.add_heading("19. 可访问性与舒适度", level=1)
    add_bullets(doc, [
        "颜色不是唯一信息：射门蓝线/花式黄线同时用线型和文字标签；",
        "镜头拉近强度可调，提供“减少动态”选项；",
        "倾斜默认关闭，提供重新校准和一键关闭；",
        "左右手模式：允许把摇杆/手势区域镜像；",
        "触控目标至少 44×44pt；",
        "字幕/提示可放大，重要提示不只靠颜色。",
    ])

    doc.add_heading("20. 验收标准", level=1)
    add_table(doc, ["类别", "标准"], [
        ["功能", "射门/爆发/花式/抢断/进球/重开全链路可完成"],
        ["输入", "球上必射门；球外直线必爆发；弯折必 Elastico"],
        ["反馈", "任何动作 100ms 内有视觉反馈"],
        ["镜头", "花式不引起 root/相机突然转向"],
        ["移动", "摇杆与手势可同时使用，不误触"],
        ["性能", "中端机 30fps 稳定，目标 60fps"],
        ["稳定", "连续 10 局无异常、无丢球权 bug"],
        ["可用性", "新手 3 分钟内完成一次射门和一次花式"],
    ])

    doc.add_heading("21. 路线图与风险", level=1)
    add_table(doc, ["阶段", "目标", "关键交付"], [
        ["P0.5", "真机可玩", "Android APK、摇杆/触摸回归、性能基线"],
        ["P1", "可展示 Demo", "UGUI、正式动作、音效、教学、结算"],
        ["P2", "可测试版本", "AI 行为树、门将、更多花式、设置/重映射"],
        ["P3", "发行评估", "联网/商业化/长期内容，另立方案"],
    ])
    add_table(doc, ["风险", "影响", "应对"], [
        ["手势误判", "射门/花式抢输入", "起点硬规则 + 二元分类 + 诊断 HUD"],
        ["触摸采样不足", "快速折线识别失败", "最少 3 点 + 放慢引导 + 可视化回放"],
        ["镜头晕动", "移动端不适", "降低拉近、动态FOV开关"],
        ["透明穹顶 overdraw", "帧率下降", "低画质关闭电路动画/减少层数"],
        ["动画素材授权", "发行风险", "保留授权记录，正式版替换/自制"],
    ])

    doc.add_heading("附录：UI 文案与参数", level=1)
    add_table(doc, ["场景", "文案"], [
        ["主按钮", "开始比赛"],
        ["副按钮", "操作说明 / 设置"],
        ["开球", "开球"],
        ["射门提示", "从球开始拖动"],
        ["花式提示", "球外画 > 或 <"],
        ["花式成功", "Elastico / Beat!"],
        ["暂停", "继续 / 重新开始 / 退出"],
        ["结算", "再来一局 / 返回主界面"],
    ])
    add_table(doc, ["参数", "建议值"], [
        ["摇杆半径", "屏高 8–10%"],
        ["球命中圈", "屏高 7% 或 95px"],
        ["爆发距离", "2.4–2.7m"],
        ["Elastico 镜头", "0.6s 子弹时间"],
        ["HUD 安全边距", "屏宽 3%"],
        ["目标帧率", "60fps / 低画质 30fps"],
    ])

    out = DOCS / "GEL_完整前端策划案.docx"
    doc.save(out)
    return out


def verify(path):
    d = Document(path)
    print(f"VERIFY {path.name}: paragraphs={len(d.paragraphs)} tables={len(d.tables)} size={path.stat().st_size}")
    text = "\n".join(p.text for p in d.paragraphs)
    assert "绿茵" in text
    assert len(d.tables) > 0
    assert path.stat().st_size > 10000


if __name__ == "__main__":
    summary = build_summary()
    frontend = build_frontend_plan()
    verify(summary)
    verify(frontend)
    print(summary)
    print(frontend)
