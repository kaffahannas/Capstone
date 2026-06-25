import os, glob

base = r'D:\LightenUp\WebsiteLightenUp\.claude\worktrees\pedantic-grothendieck-8c4b6d'
files = glob.glob(base + r'\**\*.cshtml', recursive=True)

replacements = [
    ('Â·', '·'),
    ('â†\x92', '→'),
    ('â€"', '—'),
    ('â€\x94', '—'),
    ('Â·', '·'),
]

for f in files:
    try:
        with open(f, 'r', encoding='utf-8') as fh:
            content = fh.read()
        original = content
        for old, new in replacements:
            content = content.replace(old, new)
        if content != original:
            with open(f, 'w', encoding='utf-8', newline='') as fh:
                fh.write(content)
            print('Fixed:', os.path.basename(f))
    except Exception as e:
        print('Error', f, e)

print('Done')
