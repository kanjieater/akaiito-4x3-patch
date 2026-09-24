from pathlib import Path
from datetime import datetime
import hashlib, json, shutil, sys
import dnfile
from dncil.cil.body import CilMethodBody
from dncil.cil.body.reader import CilMethodBodyReaderBytes

root = Path(sys.argv[1]).resolve()
path = root / 'AKAIITO_HD_REMASTER_Data/Managed/Assembly-CSharp.dll'
original = path.read_bytes()
p = dnfile.dnPE(data=original)
t = next(t for t in p.net.mdtables.TypeDef if str(t.TypeName) == 'boot')
m = next(x.row for x in t.MethodList if str(x.row.Name) == 'BootInit')
b = CilMethodBody(CilMethodBodyReaderBytes(p.get_data(m.Rva, 10000)))
ins = b.instructions
assert [(i.opcode.name, i.operand) for i in ins[3:7]] == [
    ('ldc.i4', 1920), ('ldc.i4', 1080), ('ldc.i4.1', None), ('call', ins[6].operand)
]
base = p.get_offset_from_rva(m.Rva)
positions = [base + i.offset for i in ins[3:7]]
new = bytearray(original)
for offset, size in zip(positions, [5, 5, 1, 5]):
    new[offset:offset + size] = b'\0' * size
path.chmod(0o666)
backup = path.with_name(path.name + '.bak-4x3-clean-' + datetime.now().strftime('%Y%m%d-%H%M%S'))
shutil.copy2(path, backup)
path.write_bytes(new)
assert path.read_bytes() == bytes(new)
manifest_path = root / 'AKAIITO-4x3-patch-manifest.json'
manifest = json.loads(manifest_path.read_text(encoding='utf8'))
manifest.append({
    'file': str(path), 'backup': str(backup),
    'change': 'Remove hardcoded BootInit SetResolution(1920,1080,true)',
    'original_sha256': hashlib.sha256(original).hexdigest(),
    'patched_sha256': hashlib.sha256(new).hexdigest(),
})
manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf8')
print('patched DLL', backup)
