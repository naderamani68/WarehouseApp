import json, sys
for label, path in (("BEFORE", sys.argv[1]), ("AFTER", sys.argv[2]), ("FIXLOG", sys.argv[3])):
    try:
        data = json.load(open(path, encoding="utf-8"))
    except Exception as e:
        print(label, "-> خطای خواندن", e); continue
    print("=== " + label + " ===")
    for item in data:
        print("   ", ascii(item))
