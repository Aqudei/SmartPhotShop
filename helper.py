import argparse
import pandas as pd
import openpyxl
import csv

input_file = "C:\\Users\\aqudei\\Downloads\\SmarkPhotShop\\LOCK_DECORATIVE_MAGNET_KEYCHAIN_BOTTLE_OPENER_SHIRT - Excel.xlsx"
skipped = ['SKU','Item Name']

if __name__ == "__main__":
    wb = openpyxl.load_workbook(input_file, read_only=True)
    ws = wb["Template"]
    last_group = None
    with open("./columns.csv", "wt", newline="") as outfile:
        writer = csv.writer(outfile)
        writer.writerow(("header", "group", 'type', "sample"))
        for row in ws.iter_rows(min_row=4):
            for c in row:
                header = c.value
                if not header:
                    break
                
                if '[' in header or ']' in header:
                    continue
                if header.strip() in skipped:
                    continue

                sample = ws.cell(c.row + 2, c.column).value
                group = ws.cell(c.row - 1, c.column).value
                if group:
                    last_group = group.strip()

                print(f"header:{header}, sample:{sample}, group:{last_group}")
                writer.writerow((header.strip(), last_group, 'System.Object', sample))
            break
