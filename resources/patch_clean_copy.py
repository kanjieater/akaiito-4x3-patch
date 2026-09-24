"""Apply the validated AKAIITO 4:3 patch to a clean game directory."""
from __future__ import annotations
import hashlib, json, shutil, struct, sys, subprocess
from datetime import datetime
from pathlib import Path
import UnityPy, dnfile
from dncil.cil.body import CilMethodBody
from dncil.cil.body.reader import CilMethodBodyReaderBytes

SUPPORTED_VERSION = 'v98_r95895-JP-CN-EN'
root = Path(sys.argv[1]).resolve()
data = root / 'AKAIITO_HD_REMASTER_Data'
if not (root/'AKAIITO_HD_REMASTER.exe').exists(): raise SystemExit(f'Not an AKAIITO clean copy: {root}')
version_file = Path(__file__).resolve().parent.parent / 'VERSION'
if version_file.exists() and version_file.read_text(encoding='utf8').strip() != SUPPORTED_VERSION:
    raise SystemExit(f'Unsupported patch version: expected {SUPPORTED_VERSION}')
backup_tag = 'bak-4x3-clean-' + datetime.now().strftime('%Y%m%d-%H%M%S')
manifest=[]
def sha(b): return hashlib.sha256(b).hexdigest()
def record(path, original, new, change):
    path.chmod(0o666)
    backup=path.with_name(path.name+'.'+backup_tag); shutil.copy2(path,backup); assert backup.read_bytes()==original
    path.write_bytes(new); assert path.read_bytes()==bytes(new)
    manifest.append({'file':str(path),'backup':str(backup),'change':change,'original_sha256':sha(original),'patched_sha256':sha(new)})
def patch_scalers(path):
    original=path.read_bytes(); new=bytearray(original); count=0; env=UnityPy.load(str(path))
    for o in env.objects:
        if o.type.name!='MonoBehaviour': continue
        raw=o.get_raw_data()
        if len(raw)==80 and struct.unpack_from('<f',raw,36)==(100.0,) and struct.unpack_from('<ff',raw,44)==(1920.0,1080.0) and struct.unpack_from('<i',raw,32)[0] in (0,1):
            idx=original.find(raw); assert idx>=0 and original.count(raw)==1
            struct.pack_into('<f',new,idx+44,1440.0); count+=1
    assert count>0, (path,count)
    record(path,original,new,f'{count} CanvasScaler reference widths 1920->1440')
def patch_level0(path):
    original=path.read_bytes(); new=bytearray(original); env=UnityPy.load(str(path)); changed=[]
    for o in env.objects:
        if o.type.name=='MonoBehaviour':
            raw=o.get_raw_data()
            if len(raw)==80 and struct.unpack_from('<f',raw,36)==(100.0,) and struct.unpack_from('<ff',raw,44)==(1920.0,1080.0) and struct.unpack_from('<i',raw,32)[0] in (0,1):
                idx=original.find(raw); assert idx>=0 and original.count(raw)==1; struct.pack_into('<f',new,idx+44,1440.0); changed.append(idx+44)
    for pid in (63,69):
        o=next(o for o in env.objects if o.path_id==pid); raw=o.get_raw_data(); assert len(raw)==108 and struct.unpack_from('<f',raw,92)==(1920.0,)
        idx=original.find(raw); assert idx>=0 and original.count(raw)==1; struct.pack_into('<f',new,idx+92,1440.0); changed.append(idx+92)
    assert len(changed)==5, changed
    record(path,original,new,f'{len(changed)} title CanvasScaler/title object widths -> 1440')
def patch_level4(path):
    original=path.read_bytes(); new=bytearray(original); env=UnityPy.load(str(path)); changed=[]
    for o in env.objects:
        if o.type.name=='MonoBehaviour':
            raw=o.get_raw_data()
            if len(raw)==80 and struct.unpack_from('<f',raw,36)==(100.0,) and struct.unpack_from('<ff',raw,44)==(1920.0,1080.0) and struct.unpack_from('<i',raw,32)[0] in (0,1):
                idx=original.find(raw); assert idx>=0 and original.count(raw)==1; struct.pack_into('<f',new,idx+44,1440.0); changed.append(idx+44)
        if o.type.name=='GameObject':
            t=o.read_typetree(check_read=False)
            if t.get('m_Name') in ('MaskL','MaskR'):
                raw=o.get_raw_data(); idx=original.find(raw); assert idx>=0 and original.count(raw)==1 and raw[-1]==1
                new[idx+len(raw)-1]=0; changed.append(idx+len(raw)-1)
    o=next(o for o in env.objects if o.path_id==1379); raw=o.get_raw_data(); idx=original.find(raw); assert struct.unpack_from('<f',raw,84)==(960.0,)
    struct.pack_into('<f',new,idx+84,720.0); changed.append(idx+84)
    assert len(changed)==14, changed
    record(path,original,new,'CanvasScalers -> 1440, MaskL/MaskR disabled, AlphaFadeCanvas centered at x=720')
def patch_dll(path):
    original=path.read_bytes(); p=dnfile.dnPE(str(path)); t=next(t for t in p.net.mdtables.TypeDef if str(t.TypeName)=='boot'); m=next(x.row for x in t.MethodList if str(x.row.Name)=='BootInit'); b=CilMethodBody(CilMethodBodyReaderBytes(p.get_data(m.Rva,10000))); ins=b.instructions
    assert [(i.opcode.name,i.operand) for i in ins[3:7]]==[('ldc.i4',1920),('ldc.i4',1080),('ldc.i4.1',None),('call',ins[6].operand)]
    base=p.get_offset_from_rva(m.Rva); positions=[base+i.offset for i in ins[3:7]]; new=bytearray(original)
    for o,n in zip(positions,[5,5,1,5]): new[o:o+n]=b'\0'*n
    del p
    record(path,original,new,'Remove hardcoded BootInit SetResolution(1920,1080,true)')

patch_level0(data/'level0'); patch_scalers(data/'level2'); patch_scalers(data/'level3'); patch_level4(data/'level4')
out=root/'AKAIITO-4x3-patch-manifest.json'; out.write_text(json.dumps(manifest,indent=2),encoding='utf8')
subprocess.run([sys.executable, str(Path(__file__).with_name('patch_clean_dll.py')), str(root)], check=True)
print(json.dumps(json.loads(out.read_text()),indent=2))
