from datetime import date, datetime
from pathlib import Path

from openpyxl import Workbook
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation

OUT = Path(r"C:\Users\pc\Desktop\作品集\GEL\Docs\GEL_学习与投递日程表.xlsx")

NAVY = "082231"
TEAL = "0B5A6E"
CYAN = "13A6C4"
LIME = "8EC43F"
LIGHT = "F2F7F9"
PALE = "E8F3F6"
WHITE = "FFFFFF"
AMBER = "FFF1D9"
GREEN = "E7F4E8"
GREY = "EEF1F3"
RED = "FBE4E4"
FONT = "微软雅黑"

thin = Side(style="thin", color="D5E1E6")
border = Border(left=thin, right=thin, top=thin, bottom=thin)


def style_range(ws, min_row, max_row, min_col, max_col, fill=None, color=None, bold=None,
                size=None, align=None, wrap=True):
    for row in ws.iter_rows(min_row=min_row, max_row=max_row, min_col=min_col, max_col=max_col):
        for cell in row:
            if fill:
                cell.fill = PatternFill("solid", fgColor=fill)
            if color:
                cell.font = Font(name=FONT, size=size or 10, bold=bold or False, color=color)
            elif size or bold is not None:
                cell.font = Font(name=FONT, size=size or 10, bold=bold or False, color="22313A")
            cell.alignment = Alignment(
                horizontal=align or "left", vertical="center", wrap_text=wrap
            )
            cell.border = border


def title(ws, text, subtitle):
    ws.merge_cells("A1:H1")
    ws["A1"] = text
    ws["A1"].fill = PatternFill("solid", fgColor=NAVY)
    ws["A1"].font = Font(name=FONT, size=20, bold=True, color=WHITE)
    ws["A1"].alignment = Alignment(vertical="center", horizontal="left", indent=1)
    ws.row_dimensions[1].height = 36

    ws.merge_cells("A2:H2")
    ws["A2"] = subtitle
    ws["A2"].fill = PatternFill("solid", fgColor=CYAN)
    ws["A2"].font = Font(name=FONT, size=10, bold=True, color=WHITE)
    ws["A2"].alignment = Alignment(vertical="center", horizontal="left", indent=1)
    ws.row_dimensions[2].height = 23


def header(ws, row, values, widths):
    for i, value in enumerate(values, start=1):
        cell = ws.cell(row=row, column=i, value=value)
        cell.fill = PatternFill("solid", fgColor=TEAL)
        cell.font = Font(name=FONT, size=9.5, bold=True, color=WHITE)
        cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
        cell.border = border
        ws.column_dimensions[get_column_letter(i)].width = widths[i - 1]
    ws.row_dimensions[row].height = 28


def status_fill(status):
    if "完成" in status or "已投" in status:
        return GREEN
    if "进行" in status:
        return AMBER
    if "截止" in status or "风险" in status:
        return RED
    return GREY


wb = Workbook()
ws = wb.active
ws.title = "总览"
ws.sheet_view.showGridLines = False
title(ws, "GEL · 学习与投递日程表", "2026-10-27 → 2026-11-10　|　游戏策划寒假实习　|　简历 / 项目 / 投递")
header(
    ws, 4,
    ["日期", "星期", "重心", "任务", "交付 / 验收", "状态", "备注"],
    [12, 8, 16, 42, 38, 12, 20],
)

rows = [
    (date(2026, 10, 27), "周二", "盘点", "现状盘点；整理已有简历、项目与作品链接", "GEL 简历版、日程表、作品链接清单", "已完成", "以当前版本为基线"),
    (date(2026, 10, 28), "周三", "项目 1 收口", "完成项目 1 接口/演示/截图；简历加入 GEL 项目", "项目 1 可演示；简历 v3 定稿", "进行中", "只补能写进简历的结果"),
    (date(2026, 10, 29), "周四", "第一批投递", "官网复核岗位；投腾讯 / 乐元素 / 完美世界", "3 家投递记录；简历 PDF 已发", "待执行", "投前确认岗位仍开放"),
    (date(2026, 10, 30), "周五", "第一批截止", "补齐投递记录；做一次 30 分钟模拟面试", "投递表；模拟面试自评", "截止", "第一批不要拖到周末"),
    (date(2026, 10, 31), "周六", "项目 3", "发布项目 3，拿到公开链接；简历 v4 框架", "项目 3 链接；v4 结构", "待执行", "先有链接再写简历"),
    (date(2026, 11, 1), "周日", "复盘/休息", "复盘第一批反馈；整理 3 个可量化成果", "复盘 1 页；数字清单", "待执行", "不要继续海投"),
    (date(2026, 11, 2), "周一", "作品集", "把 GEL 网页 / 策划案 / 总结报告整理成统一入口", "作品集链接", "待执行", "面试时 1 个链接够用"),
    (date(2026, 11, 3), "周二", "简历 v4", "把项目 1/3/4 的成果改成可验证数字", "简历 v4 初稿", "待执行", "删空泛描述"),
    (date(2026, 11, 4), "周三", "游戏策划", "竞品拆解 1 页；准备系统/数值面试题", "竞品拆解；答题卡", "待执行", "用一个竞品讲透"),
    (date(2026, 11, 5), "周四", "简历 v4", "简历 v4 排版；DOCX + PDF 双版本", "单页简历 v4", "待执行", "保持单页"),
    (date(2026, 11, 6), "周五", "第二批准备", "整理 16 家公司清单；官网确认岗位状态", "第二批投递清单", "待执行", "按优先级分档"),
    (date(2026, 11, 7), "周六", "模拟面试", "项目追问 + Unity/C# + 数学；录音复盘", "模拟面试记录", "待执行", "只补 2 个薄弱点"),
    (date(2026, 11, 8), "周日", "休息/复盘", "休息；检查作品链接、简历附件、邮箱签名", "投递前检查清单", "待执行", "不要临时改大方向"),
    (date(2026, 11, 9), "周一", "第二批预演", "按岗位定制 2 份简历；准备 1 分钟项目讲法", "定制简历；讲法录音", "待执行", "不同岗位不同 bullet"),
    (date(2026, 11, 10), "周二", "第二批投递", "投心动 / 腾讯 / 字节 / B 站等第二批", "投递记录；跟进日期", "截止", "投完即记录"),
]

for r, (d, weekday, focus, task, deliverable, status, note) in enumerate(rows, start=5):
    values = [d, weekday, focus, task, deliverable, status, note]
    for c, value in enumerate(values, start=1):
        cell = ws.cell(row=r, column=c, value=value)
        cell.font = Font(name=FONT, size=9, color="22313A")
        cell.alignment = Alignment(horizontal="center" if c in (1, 2, 6) else "left",
                                   vertical="center", wrap_text=True)
        cell.border = border
    ws.cell(row=r, column=1).number_format = "mm-dd"
    ws.cell(row=r, column=6).fill = PatternFill("solid", fgColor=status_fill(status))
    ws.row_dimensions[r].height = 42
    if focus in ("第一批截止", "第二批投递"):
        for c in range(1, 8):
            ws.cell(row=r, column=c).fill = PatternFill("solid", fgColor=RED if focus == "第一批截止" else GREEN)

ws.freeze_panes = "A5"
ws.auto_filter.ref = f"A4:G{4 + len(rows)}"
ws.print_title_rows = "1:4"
ws.page_setup.orientation = "landscape"
ws.page_setup.fitToWidth = 1
ws.sheet_properties.pageSetUpPr.fitToPage = True

# Legend
legend_row = 4 + len(rows) + 2
ws.merge_cells(start_row=legend_row, start_column=1, end_row=legend_row, end_column=7)
ws.cell(row=legend_row, column=1,
        value="图例：绿色=完成/已投；黄色=进行中；红色=截止/风险。投递前务必到官网复核岗位状态。").font = Font(
    name=FONT, size=8.5, color="556973", italic=True)
ws.cell(row=legend_row, column=1).alignment = Alignment(wrap_text=True, vertical="center")

# ---------- 72h ----------
ws2 = wb.create_sheet("72小时")
ws2.sheet_view.showGridLines = False
title(ws2, "72 小时行动表", "10/27 → 10/30　|　把项目和简历先推到一个可投递状态")
header(ws2, 4, ["日期", "时间段", "任务", "交付", "完成"], [12, 14, 48, 42, 10])
rows2 = [
    (date(2026, 10, 27), "晚上", "盘点已有简历，确定 GEL 项目在简历中的位置", "简历素材清单", "☐"),
    (date(2026, 10, 27), "晚上", "整理作品链接：网页 / 策划案 / 总结报告", "作品集链接", "☐"),
    (date(2026, 10, 28), "上午", "项目 1 收口：接口、演示、截图", "可演示版本", "☐"),
    (date(2026, 10, 28), "下午", "简历加入 GEL：一句话 + 3 bullets + 3 个数字", "简历 v3", "☐"),
    (date(2026, 10, 28), "晚上", "录 1 分钟项目讲法；检查是否像“我做过的”", "讲法录音", "☐"),
    (date(2026, 10, 29), "上午", "官网复核腾讯 / 乐元素 / 完美世界岗位", "3 个岗位链接", "☐"),
    (date(2026, 10, 29), "下午", "投第一批；记录公司 / 岗位 / 时间 / 入口", "投递记录", "☐"),
    (date(2026, 10, 30), "上午", "补投 / 检查附件命名与 PDF 可读性", "投递完成", "☐"),
    (date(2026, 10, 30), "下午", "30 分钟模拟面试：项目追问 + 为什么没做完", "自评 3 条", "☐"),
]
for r, (d, period, task, deliverable, done) in enumerate(rows2, start=5):
    for c, value in enumerate((d, period, task, deliverable, done), start=1):
        cell = ws2.cell(row=r, column=c, value=value)
        cell.font = Font(name=FONT, size=9, color="22313A")
        cell.alignment = Alignment(horizontal="center" if c in (1, 2, 5) else "left",
                                   vertical="center", wrap_text=True)
        cell.border = border
    ws2.cell(row=r, column=1).number_format = "mm-dd"
    ws2.row_dimensions[r].height = 31
ws2.freeze_panes = "A5"
ws2.auto_filter.ref = f"A4:E{4 + len(rows2)}"

# ---------- 11/10 ----------
ws3 = wb.create_sheet("11-10前")
ws3.sheet_view.showGridLines = False
title(ws3, "11/10 前的准备清单", "第二批投递前：链接、数字、定制简历、模拟面试")
header(ws3, 4, ["优先级", "事项", "完成标准", "状态", "截止"], [10, 42, 48, 12, 12])
rows3 = [
    ("P0", "项目 3 发布并拿到公开链接", "链接可打开、无权限、手机可看", "未开始", "11/06"),
    ("P0", "简历 v4：项目 1/3/4 的成果数字化", "每条 bullet 至少 1 个数字或可验证结果", "未开始", "11/05"),
    ("P0", "第二批 16 家公司清单", "按优先级分档；官网状态已确认", "未开始", "11/06"),
    ("P1", "GEL 网页 / 策划案 / 总结报告统一入口", "一个链接可访问全部材料", "未开始", "11/02"),
    ("P1", "竞品拆解 1 页", "核心循环 / 输入 / 反馈 / 可借鉴点", "未开始", "11/04"),
    ("P1", "系统/数值面试题 30 题", "每题能讲 60 秒", "未开始", "11/07"),
    ("P1", "模拟面试 2 次", "录音；每次只补 2 个薄弱点", "未开始", "11/09"),
    ("P2", "岗位定制简历 2 份", "系统向 / 数值向各 1 份", "未开始", "11/09"),
]
for r, row in enumerate(rows3, start=5):
    for c, value in enumerate(row, start=1):
        cell = ws3.cell(row=r, column=c, value=value)
        cell.font = Font(name=FONT, size=9, color="22313A")
        cell.alignment = Alignment(horizontal="center" if c in (1, 4, 5) else "left",
                                   vertical="center", wrap_text=True)
        cell.border = border
    ws3.row_dimensions[r].height = 34
    ws3.cell(row=r, column=4).fill = PatternFill("solid", fgColor=GREY)
ws3.freeze_panes = "A5"
ws3.auto_filter.ref = f"A4:E{4 + len(rows3)}"

# ---------- 投递记录 ----------
ws4 = wb.create_sheet("投递记录")
ws4.sheet_view.showGridLines = False
title(ws4, "投递记录", "每投一家就填一行；状态每周更新一次")
header(ws4, 4, ["批次", "公司", "岗位", "入口 / 链接", "投递日期", "状态", "跟进日期", "备注"],
       [9, 18, 22, 34, 12, 12, 12, 26])
companies = [
    ("第一批", "腾讯", "游戏策划（系统/数值）"),
    ("第一批", "乐元素", "游戏策划"),
    ("第一批", "完美世界", "游戏策划"),
    ("第二批", "心动", "游戏策划"),
    ("第二批", "腾讯", "游戏策划"),
    ("第二批", "字节", "游戏策划"),
    ("第二批", "B 站", "游戏策划"),
]
for r, (batch, company, role) in enumerate(companies, start=5):
    values = [batch, company, role, "", "", "未投递", "", ""]
    for c, value in enumerate(values, start=1):
        cell = ws4.cell(row=r, column=c, value=value)
        cell.font = Font(name=FONT, size=9, color="22313A")
        cell.alignment = Alignment(horizontal="center" if c in (1, 5, 6, 7) else "left",
                                   vertical="center", wrap_text=True)
        cell.border = border
    ws4.cell(row=r, column=5).number_format = "mm-dd"
    ws4.cell(row=r, column=7).number_format = "mm-dd"
    ws4.row_dimensions[r].height = 28
for r in range(5 + len(companies), 25):
    for c in range(1, 9):
        cell = ws4.cell(row=r, column=c)
        cell.border = border
        cell.font = Font(name=FONT, size=9)
        cell.alignment = Alignment(vertical="center", wrap_text=True)
ws4.freeze_panes = "A5"
ws4.auto_filter.ref = "A4:H24"

# ---------- 每周节奏 ----------
ws5 = wb.create_sheet("每周节奏")
ws5.sheet_view.showGridLines = False
title(ws5, "每周节奏", "项目、学习、求职三线并行；每周只追 3 个优先级")
header(ws5, 4, ["星期", "上午 / 白天", "晚上", "产出"], [10, 44, 44, 34])
weekly = [
    ("周一", "Unity / 项目代码复盘", "系统或数值题 2 道", "3 个能讲清楚的系统"),
    ("周二", "C# / 数学基础", "手写向量题 5 道", "错题本"),
    ("周三", "竞品拆解 / 游戏设计", "写 1 页拆解", "竞品分析"),
    ("周四", "项目补强（只做 1 个改动）", "录 1 分钟讲法", "可演示改动"),
    ("周五", "模拟面试 / 简历复盘", "投递或跟进", "面试录音"),
    ("周六", "作品集 / 简历更新", "整理投递记录", "新版简历或作品"),
    ("周日", "休息 / 复盘", "下周 3 个优先级", "周计划"),
]
for r, row in enumerate(weekly, start=5):
    for c, value in enumerate(row, start=1):
        cell = ws5.cell(row=r, column=c, value=value)
        cell.font = Font(name=FONT, size=9, color="22313A")
        cell.alignment = Alignment(horizontal="center" if c == 1 else "left",
                                   vertical="center", wrap_text=True)
        cell.border = border
    ws5.row_dimensions[r].height = 35
ws5.freeze_panes = "A5"

# ---------- checklist ----------
ws6 = wb.create_sheet("投递检查")
ws6.sheet_view.showGridLines = False
title(ws6, "投递前检查", "每次投递前 60 秒过一遍")
header(ws6, 4, ["检查项", "标准", "通过"], [46, 56, 12])
checks = [
    ("岗位状态", "官网/招聘平台确认仍在招；不是过期转发"),
    ("简历文件", "PDF 单页，命名清楚；DOCX 留底但投 PDF"),
    ("简历内容", "与岗位方向匹配；至少 3 个数字或可验证成果"),
    ("项目链接", "网页/策划案/总结报告可打开，手机可看"),
    ("邮件/表单", "称呼、岗位名、附件、签名检查"),
    ("投递记录", "公司/岗位/日期/入口/状态已记录"),
    ("跟进", "7 天后无反馈再跟进一次；不要频繁催"),
]
for r, (item, standard) in enumerate(checks, start=5):
    for c, value in enumerate((item, standard, "☐"), start=1):
        cell = ws6.cell(row=r, column=c, value=value)
        cell.font = Font(name=FONT, size=9, color="22313A")
        cell.alignment = Alignment(horizontal="center" if c == 3 else "left",
                                   vertical="center", wrap_text=True)
        cell.border = border
    ws6.row_dimensions[r].height = 32
ws6.freeze_panes = "A5"

# Data validation for status columns.
dv = DataValidation(type="list", formula1='"未开始,进行中,已完成,已投递,待跟进,已约面,已结束"', allow_blank=True)
ws.add_data_validation(dv)
dv.add(f"F5:F{4 + len(rows)}")
dv2 = DataValidation(type="list", formula1='"未投递,已投递,待跟进,已约面,已结束"', allow_blank=True)
ws4.add_data_validation(dv2)
dv2.add(f"F5:F24")

wb.save(OUT)
print(OUT)
