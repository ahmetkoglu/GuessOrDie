const net = require('net');

const code = String.raw`
import bpy, math
from mathutils import Vector

for obj in list(bpy.context.scene.objects):
    n = obj.name.lower()
    if 'sword' in n or 'kilic' in n or 'kılıç' in n or 'blade' in n or 'guard' in n or 'grip' in n:
        bpy.data.objects.remove(obj, do_unlink=True)

old = bpy.data.collections.get('LowPoly_Toon_Mike_Wazowski')
if old:
    for obj in list(old.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.collections.remove(old)

def toon_mat(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    toon = nodes.new('ShaderNodeBsdfToon')
    toon.inputs['Color'].default_value = color
    toon.inputs['Size'].default_value = 0.48
    toon.inputs['Smooth'].default_value = 0.035
    mat.node_tree.links.new(toon.outputs['BSDF'], out.inputs['Surface'])
    return mat

m_green = toon_mat('Mike_Toon_Lime_Green', (0.48, 0.78, 0.14, 1))
m_dark = toon_mat('Mike_Toon_Darker_Green', (0.23, 0.50, 0.08, 1))
m_light = toon_mat('Mike_Toon_YellowGreen_Highlight', (0.78, 0.95, 0.20, 1))
m_eye = toon_mat('Mike_Toon_Warm_Eye_White', (0.93, 0.98, 0.84, 1))
m_iris = toon_mat('Mike_Toon_Turquoise_Iris', (0.00, 0.68, 0.58, 1))
m_black = toon_mat('Mike_Toon_Black_Pupil_Mouth', (0.015, 0.012, 0.010, 1))
m_teeth = toon_mat('Mike_Toon_Cream_Teeth_Claws', (0.96, 0.91, 0.73, 1))
m_horn = toon_mat('Mike_Toon_Warm_Grey_Horns', (0.62, 0.58, 0.45, 1))

col = bpy.data.collections.new('LowPoly_Toon_Mike_Wazowski')
bpy.context.scene.collection.children.link(col)

def link(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj

def flat(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try: bpy.ops.object.shade_flat()
    except Exception: pass
    obj.select_set(False)
    return obj

def ico(name, loc, scale, mat, sub=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub, radius=1, location=loc)
    obj = bpy.context.object; obj.name = name; obj.scale = scale
    obj.data.materials.append(mat); link(obj); return flat(obj)

def uv(name, loc, scale, mat, seg=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=1, location=loc)
    obj = bpy.context.object; obj.name = name; obj.scale = scale
    obj.data.materials.append(mat); link(obj); return flat(obj)

def cyl(name, a, b, r, mat, v=6):
    a=Vector(a); b=Vector(b); d=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=v, radius=r, depth=d.length, location=(a+b)/2)
    obj=bpy.context.object; obj.name=name; obj.data.materials.append(mat)
    obj.rotation_euler=d.to_track_quat('Z','Y').to_euler(); link(obj); return flat(obj)

def cone(name, a, b, r1, r2, mat, v=6):
    a=Vector(a); b=Vector(b); d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=v, radius1=r1, radius2=r2, depth=d.length, location=(a+b)/2)
    obj=bpy.context.object; obj.name=name; obj.data.materials.append(mat)
    obj.rotation_euler=d.to_track_quat('Z','Y').to_euler(); link(obj); return flat(obj)

body = ico('Mike_LowPoly_Round_Body', (0,0,1.45), (1.38,1.05,1.55), m_green, 3)
highlight = ico('Mike_LowPoly_YellowGreen_Belly_Highlight', (-0.22,-0.82,1.72), (0.55,0.06,0.78), m_light, 2)
highlight.rotation_euler[0] = math.radians(84)

for name, loc, scale, mat, seg in [
    ('Mike_One_Giant_Eye_White',(0,-1.02,1.92),(0.62,0.16,0.62),m_eye,14),
    ('Mike_Turquoise_Iris',(0,-1.15,1.92),(0.25,0.035,0.25),m_iris,12),
    ('Mike_Black_Pupil',(0,-1.19,1.92),(0.105,0.018,0.105),m_black,10),
    ('Mike_Eye_Sparkle',(-0.10,-1.205,2.05),(0.045,0.009,0.045),toon_mat('Mike_Toon_Eye_Glint',(1,1,0.92,1)),8),
    ('Mike_Big_Black_Crescent_Mouth',(0,-1.18,1.18),(0.68,0.035,0.29),m_black,14),
]:
    o = uv(name, loc, scale, mat, seg, 5)
    o.rotation_euler[0] = math.radians(88)

cyl('Mike_Green_Upper_Smile_Lip', (-0.67,-1.205,1.33), (0.67,-1.205,1.33), 0.035, m_light, 8)
cyl('Mike_Dark_Green_Lower_Smile_Shadow', (-0.43,-1.21,1.02), (0.43,-1.21,1.02), 0.025, m_dark, 8)

for i,x in enumerate([-0.48,-0.32,-0.16,0.0,0.16,0.32,0.48]):
    cone(f'Mike_Top_Tooth_{i+1}', (x,-1.235,1.31), (x+0.025,-1.235,1.18), 0.045, 0.012, m_teeth, 5)
for i,x in enumerate([-0.31,-0.10,0.11,0.32]):
    cone(f'Mike_Bottom_Tooth_{i+1}', (x,-1.235,1.04), (x-0.02,-1.235,1.15), 0.038, 0.010, m_teeth, 5)

cone('Mike_Left_Horn', (-0.62,-0.04,2.78), (-0.88,-0.07,3.23), 0.14, 0.02, m_horn, 6)
cone('Mike_Right_Horn', (0.62,-0.04,2.78), (0.88,-0.07,3.23), 0.14, 0.02, m_horn, 6)

cyl('Mike_Left_Upper_Arm_Raised', (-1.12,-0.02,1.86), (-1.95,-0.08,2.35), 0.075, m_green)
cyl('Mike_Left_Forearm_Raised', (-1.95,-0.08,2.35), (-2.33,-0.12,2.93), 0.065, m_green)
ico('Mike_Left_Open_Palm', (-2.34,-0.13,2.98), (0.15,0.08,0.13), m_green, 1)
for i,ang in enumerate([-52,-22,12,42]):
    start = Vector((-2.34,-0.13,3.03))
    end = start + Vector((0.28*math.sin(math.radians(ang)), -0.02, 0.25*math.cos(math.radians(ang))))
    cyl(f'Mike_Left_Finger_{i+1}', start, end, 0.032, m_green, 5)

cyl('Mike_Right_Upper_Arm_Side', (1.13,-0.02,1.72), (1.83,-0.07,1.45), 0.075, m_green)
cyl('Mike_Right_Forearm_Side', (1.83,-0.07,1.45), (2.25,-0.10,1.22), 0.065, m_green)
ico('Mike_Right_Palm', (2.32,-0.12,1.16), (0.16,0.08,0.12), m_green, 1)
for i,off in enumerate([-0.12,0.0,0.12]):
    cyl(f'Mike_Right_Finger_{i+1}', (2.37,-0.13,1.17+off), (2.62,-0.14,1.12+off*0.5), 0.027, m_green, 5)

cyl('Mike_Left_Leg', (-0.45,0.03,0.10), (-0.72,-0.03,-0.88), 0.085, m_green)
cyl('Mike_Right_Leg', (0.45,0.03,0.10), (0.72,-0.03,-0.88), 0.085, m_green)
ico('Mike_Left_Wide_Foot', (-0.87,-0.36,-0.92), (0.38,0.20,0.11), m_green, 1)
ico('Mike_Right_Wide_Foot', (0.87,-0.36,-0.92), (0.38,0.20,0.11), m_green, 1)
for side,sx in [('Left',-0.87),('Right',0.87)]:
    for i,dx in enumerate([-0.18,0,0.18]):
        cone(f'Mike_{side}_Toenail_{i+1}', (sx+dx,-0.53,-0.91), (sx+dx,-0.72,-0.91), 0.035, 0.005, m_teeth, 5)

ico('Mike_Left_Arm_Shoulder_Shadow', (-1.08,-0.06,1.82), (0.16,0.04,0.16), m_dark, 1).rotation_euler[0]=math.radians(88)
ico('Mike_Right_Arm_Shoulder_Shadow', (1.08,-0.06,1.68), (0.16,0.04,0.16), m_dark, 1).rotation_euler[0]=math.radians(88)

outline = body.copy(); outline.data = body.data.copy(); outline.name = 'Mike_Black_InvertedHull_Outline_Body'
outline.scale = (body.scale.x*1.035, body.scale.y*1.035, body.scale.z*1.035)
outline.data.materials.clear(); outline.data.materials.append(toon_mat('Mike_Toon_Ink_Outline',(0,0,0,1)))
outline.location.y += 0.025; col.objects.link(outline)

cam = bpy.data.objects.get('Camera')
if not cam:
    bpy.ops.object.camera_add(); cam=bpy.context.object
cam.location=(4.1,-6.2,2.8); cam.rotation_euler=(math.radians(63),0,math.radians(37)); bpy.context.scene.camera=cam

for obj in list(bpy.context.scene.objects):
    obj.select_set(obj.type == 'LIGHT')
bpy.ops.object.delete()
bpy.ops.object.light_add(type='AREA', location=(0,-4,5)); key=bpy.context.object; key.name='Mike_Toon_Key_Area_Light'; key.data.energy=450; key.data.size=5
bpy.ops.object.light_add(type='POINT', location=(-3,-3,3)); rim=bpy.context.object; rim.name='Mike_Toon_Soft_Fill_Light'; rim.data.energy=90

try:
    engines=[i.identifier for i in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    bpy.context.scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
except Exception: pass
bpy.context.scene.view_settings.view_transform='Standard'
bpy.context.scene.view_settings.look='Medium High Contrast'
bpy.context.scene.render.use_freestyle=True

for obj in bpy.context.scene.objects: obj.select_set(False)
for obj in col.objects: obj.select_set(True)
bpy.context.view_layer.objects.active=body
print('DONE Mike Wazowski low-poly toon generated. Collection objects:', len(col.objects))
`;

const payload = JSON.stringify({ type: 'execute_code', params: { code } });
const c = net.createConnection(9876, '127.0.0.1', () => c.write(payload));
let d = '';
c.setTimeout(180000);
c.on('data', x => {
  d += x;
  try { JSON.parse(d); console.log(d); c.end(); } catch (e) {}
});
c.on('timeout', () => { console.log(d || 'TIMEOUT'); c.destroy(); process.exit(1); });
c.on('error', e => { console.error(e); process.exit(1); });