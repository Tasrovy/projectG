#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Excel文本替换工具 - 交互式版本
"""

import pandas as pd
import json
import os


class InteractiveExcelReplacer:
    def __init__(self):
        self.rules = {}

    def load_rules_from_file(self):
        """交互式加载规则文件"""
        while True:
            print("\n请选择规则配置方式:")
            print("1. 使用现有JSON文件")
            print("2. 手动输入替换规则")
            print("3. 使用示例规则")

            choice = input("请输入选项 (1/2/3): ").strip()

            if choice == '1':
                files = [f for f in os.listdir('.') if f.endswith(('.json', '.txt'))]
                if files:
                    print("\n找到的配置文件:")
                    for i, f in enumerate(files):
                        print(f"  {i + 1}. {f}")
                    file_choice = input("请输入文件名或序号: ").strip()

                    # 处理序号或文件名
                    if file_choice.isdigit():
                        idx = int(file_choice) - 1
                        if 0 <= idx < len(files):
                            file_choice = files[idx]

                    if os.path.exists(file_choice):
                        self.load_rules(file_choice)
                        break
                    else:
                        print("文件不存在，请重试")
                else:
                    print("当前文件夹没有配置文件，请选择其他方式")

            elif choice == '2':
                self.manual_input_rules()
                break

            elif choice == '3':
                self.use_sample_rules()
                break
            else:
                print("无效选项，请重新输入")

    def load_rules(self, rules_file):
        """加载规则文件"""
        try:
            if rules_file.endswith('.json'):
                with open(rules_file, 'r', encoding='utf-8') as f:
                    self.rules = json.load(f)
            elif rules_file.endswith('.txt'):
                with open(rules_file, 'r', encoding='utf-8') as f:
                    for line in f:
                        line = line.strip()
                        if line and not line.startswith('#'):
                            if '=>' in line:
                                old, new = line.split('=>', 1)
                                self.rules[old.strip()] = new.strip()

            print(f"\n成功加载 {len(self.rules)} 条替换规则:")
            for old, new in list(self.rules.items())[:5]:  # 显示前5条
                print(f"  '{old}' -> '{new}'")
            if len(self.rules) > 5:
                print(f"  ... 等{len(self.rules)}条规则")

        except Exception as e:
            print(f"加载规则失败: {e}")
            self.rules = {}

    def manual_input_rules(self):
        """手动输入替换规则"""
        print("\n请输入替换规则（每行格式: 原文本=>新文本）")
        print("输入空行结束:")

        while True:
            line = input("> ").strip()
            if not line:
                break
            if '=>' in line:
                old, new = line.split('=>', 1)
                self.rules[old.strip()] = new.strip()
                print(f"已添加: '{old.strip()}' -> '{new.strip()}'")
            else:
                print("格式错误，请使用 '原文本=>新文本' 格式")

        if self.rules:
            print(f"\n共添加 {len(self.rules)} 条规则")

    def use_sample_rules(self):
        """使用示例规则"""
        self.rules = {
            "属性一": "攻击属性",
            "属性二": "防御属性",
            "属性三": "辅助属性",
            "火": "火焰",
            "水": "水流",
            "风": "风暴",
            "雷": "雷电"
        }
        print("\n已加载示例规则:")
        for old, new in self.rules.items():
            print(f"  '{old}' -> '{new}'")

    def select_excel_file(self):
        """选择Excel文件"""
        print("\n当前文件夹中的Excel文件:")
        excel_files = [f for f in os.listdir('.') if f.endswith(('.xlsx', '.xls'))]

        if not excel_files:
            print("未找到Excel文件，请将Excel文件放到当前文件夹")
            file_path = input("请输入完整的Excel文件路径: ").strip()
            return file_path

        for i, f in enumerate(excel_files):
            print(f"  {i + 1}. {f}")

        while True:
            choice = input("请选择文件序号 (或直接输入文件名): ").strip()
            if choice.isdigit():
                idx = int(choice) - 1
                if 0 <= idx < len(excel_files):
                    return excel_files[idx]
            elif os.path.exists(choice):
                return choice
            else:
                print("文件不存在，请重新选择")

    def select_column(self, df):
        """选择要处理的列"""
        print(f"\n可用的列:")
        for i, col in enumerate(df.columns):
            print(f"  {i}. {col}")

        while True:
            choice = input("请输入列名或列序号: ").strip()

            # 检查是否为序号
            if choice.isdigit():
                idx = int(choice)
                if 0 <= idx < len(df.columns):
                    return df.columns[idx]

            # 检查是否为列名
            if choice in df.columns:
                return choice

            print("列不存在，请重新选择")

    def process_excel(self):
        """处理Excel文件"""
        try:
            # 选择Excel文件
            excel_file = self.select_excel_file()
            if not os.path.exists(excel_file):
                print(f"文件不存在: {excel_file}")
                return

            # 读取Excel
            df = pd.read_excel(excel_file)
            print(f"\n成功读取文件: {excel_file}")
            print(f"总行数: {len(df)}")

            # 选择列
            column = self.select_column(df)
            print(f"已选择列: {column}")

            # 显示前几行数据
            print(f"\n前5行数据预览:")
            for idx, value in df[column].head().items():
                print(f"  第{idx + 1}行: {value}")

            # 询问处理方式
            print("\n处理方式:")
            print("1. 直接替换原列")
            print("2. 创建新列")
            mode = input("请选择 (1/2): ").strip()

            # 执行替换
            if mode == '2':
                new_col_name = input("请输入新列名 (留空自动生成): ").strip()
                if not new_col_name:
                    new_col_name = f"{column}_replaced"
                df[new_col_name] = df[column].apply(self.replace_text)
                print(f"\n创建新列: {new_col_name}")
            else:
                df[column] = df[column].apply(self.replace_text)
                print("\n直接替换原列")

            # 保存文件
            name, ext = os.path.splitext(excel_file)
            output_file = f"{name}_replaced{ext}"
            df.to_excel(output_file, index=False)

            print(f"\n✓ 处理完成！文件已保存到: {output_file}")

            # 显示统计信息
            print(f"\n处理统计:")
            print(f"  总行数: {len(df)}")
            print(f"  处理列: {column}")

            # 显示替换示例
            print(f"\n替换示例:")
            for idx, row in df.head().iterrows():
                if mode == '2':
                    if row[column] != row[new_col_name]:
                        print(f"  原: {row[column]}")
                        print(f"  新: {row[new_col_name]}\n")
                else:
                    print(f"  第{idx + 1}行: {row[column]}")

        except Exception as e:
            print(f"处理出错: {e}")

    def replace_text(self, text):
        """执行文本替换"""
        if not isinstance(text, str):
            return text

        result = text
        for old, new in self.rules.items():
            result = result.replace(old, new)
        return result

    def run(self):
        """主流程"""
        print("=" * 50)
        print("Excel文本替换工具 - 交互式版本")
        print("=" * 50)

        # 加载规则
        self.load_rules_from_file()

        if not self.rules:
            print("没有加载任何规则，程序退出")
            return

        # 处理Excel
        self.process_excel()


if __name__ == "__main__":
    app = InteractiveExcelReplacer()
    app.run()