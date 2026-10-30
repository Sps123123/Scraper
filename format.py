import csv
import re

INPUT_FILE = "Testing - Sheet19 (1) (1).csv"
OUTPUT_FILE = "Testing - Sheet19 - formatted.csv"

def format_phone(raw):
    if raw is None:
        return ""
    s = raw.strip()
    if not s:
        return ""

    # Preserve extensions like "ext. 102"
    ext = ""
    m = re.search(r'(?i)\s*(?:ext\.?|x)\s*(\d+)\s*$', s)
    if m:
        ext = f" ext. {m.group(1)}"
        s = s[:m.start()].strip()

    digits = re.sub(r"\D", "", s)

    # Leave non-US/international numbers as-is
    if s.startswith("+") and not s.startswith("+1"):
        return s + ext

    # US/Canada 10-digit
    if len(digits) == 10:
        return f"+1 {digits[0:3]} {digits[3:6]} {digits[6:10]}{ext}"

    # US/Canada with leading 1
    if len(digits) == 11 and digits.startswith("1"):
        return f"+1 {digits[1:4]} {digits[4:7]} {digits[7:11]}{ext}"

    # Anything else: keep original text
    return s + ext


with open(INPUT_FILE, "r", encoding="utf-8-sig", newline="") as fin, \
     open(OUTPUT_FILE, "w", encoding="utf-8-sig", newline="") as fout:

    reader = csv.DictReader(fin)
    writer = csv.DictWriter(fout, fieldnames=reader.fieldnames)
    writer.writeheader()

    for row in reader:
        row["Phone number"] = format_phone(row.get("Phone number", ""))
        writer.writerow(row)

print(f"Done: {OUTPUT_FILE}")