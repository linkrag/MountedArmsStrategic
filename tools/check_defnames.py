# Fails if any non-abstract ThingDef defName ends in a digit (RimWorld config error:
# "ends with a numerical digit, which is not allowed on ThingDefs"; Blueprint_/Frame_ inherit it).
# usage: python tools/check_defnames.py
import glob, os, sys
import xml.etree.ElementTree as ET
os.chdir(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
bad = []
for f in glob.glob('**/*.xml', recursive=True):
    root = ET.parse(f).getroot()
    if root.tag != 'Defs':
        continue
    for d in root:
        if d.tag == 'ThingDef' and d.get('Abstract') != 'True':
            name = d.findtext('defName')
            if name and name[-1].isdigit():
                bad.append(f + ': ' + name)
print('\n'.join(bad) if bad else 'defNames OK')
sys.exit(1 if bad else 0)
