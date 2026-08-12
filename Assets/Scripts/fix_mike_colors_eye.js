const net = require('net');

const code = String.raw`
import bpy, math

def mat(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = color
    m.use_nodes = True
    nodes = m.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    toon = nodes.new('ShaderNodeBsdfToon')
    toon.inputs['Color'].default_value = color
    toon.inputs['Size'].default_value = 0.55
    toon.inputs['Smooth'].default_value = 0.045
    m.node_tree.links.new(toon.outputs['BSDF'], out.inputs['Surface'])
    return m

m_green = mat('Mike_FIXED_Bright_Lime_Toon', (0.54, 0.86, 0.16, 1))
m_dark = mat('Mike_FIXED_Dark_Green_Details', (0.18, 0.42, 0.05, 1))
m_light = mat('Mike_FIXED_Yellow_Green_Highlight', (0.82, 0.98, 0.24, 1))
m_eye = mat('Mike_FIXED_Warm_Eye_White', (0.96, 1.0, 0.86, 1))
m_iris = mat('Mike_FIXED_Teal_Iris', (0.0, 0.72, 0.62, 1))
m_iris2 = mat('Mike_FIXED_Dark_Teal_Iris_Ring', (0.0, 0.28, 0.24, 1))
m_black = mat('Mike_FIXED_Black', (0.005, 0.004, 0.003, 1))
m_white = mat('Mike_FIXED_White_Glint', (1, 1, 0.95, 1))
m_cream = mat('Mike_FIXED_Cream', (0.96, 0.91, 0.72, 1))
m_horn = mat('Mike_FIXED_Grey_Horn', (0.62, 0.58, 0.45, 1))

# Remove black inverted hull that was covering the green body from the camera side.
for name in ['Mike_Black_InvertedHull_Outline_Body']:
    obj = bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj, do_unlink=True)

# Restore readable Mike colors.
for obj in bpy.context.scene.objects:
    if not obj.name.startswith('Mike_') or not hasattr(obj.data, 'materials'):
        continue
    low = obj.name.lower()
    if any(k in low for k in ['body', 'arm', 'leg', 'palm', 'finger', 'foot']):
        obj.data.materials.clear(); obj.data.materials.append(m_green)
    if 'highlight' in low or 'upper_smile_lip' in low:
        obj.data.materials.clear(); obj.data.materials.append(m_light)
    if 'shadow' in low or 'lower_smile' in low:
        obj.data.materials.clear(); obj.data.materials.append(m_dark)
    if 'horn' in low:
        obj.data.materials.clear(); obj.data.materials.append(m_horn)
    if 'tooth' in low or 'toenail' in low:
        obj.data.materials.clear(); obj.data.materials.append(m_cream)

# Rebuild eye detail objects so they sit clearly on top/front.
for obj in list(bpy.context.scene.objects):
    if obj.name.startswith('Mike_EyeDetail_'):
        bpy.data.objects.remove(obj, do_unlink=True)

def add_eye_part(name, loc, scale, material, seg=24, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.rotation_euler[0] = math.radians(88)
    obj.data.materials.append(material)
    bpy.ops.object.shade_flat()
    return obj

add_eye_part('Mike_EyeDetail_Clean_Eyeball', (0, -1.245, 1.92), (0.66, 0.026, 0.66), m_eye, 24, 10)
add_eye_part('Mike_EyeDetail_Dark_Outer_Iris_Ring', (0, -1.275, 1.92), (0.285, 0.014, 0.285), m_iris2, 24, 6)
add_eye_part('Mike_EyeDetail_Teal_Iris', (0, -1.292, 1.92), (0.235, 0.012, 0.235), m_iris, 24, 6)
add_eye_part('Mike_EyeDetail_Dark_Inner_Iris', (0, -1.306, 1.92), (0.145, 0.010, 0.145), m_iris2, 18, 5)
add_eye_part('Mike_EyeDetail_Black_Pupil', (0, -1.318, 1.92), (0.085, 0.008, 0.085), m_black, 18, 5)
add_eye_part('Mike_EyeDetail_Large_Glint', (-0.105, -1.326, 2.045), (0.050, 0.005, 0.050), m_white, 12, 4)
add_eye_part('Mike_EyeDetail_Small_Glint', (0.065, -1.327, 1.995), (0.023, 0.004, 0.023), m_white, 10, 4)
add_eye_part('Mike_EyeDetail_Upper_Green_Eyelid', (0, -1.235, 2.265), (0.49, 0.018, 0.060), m_light, 18, 4)
add_eye_part('Mike_EyeDetail_Lower_Green_Eyelid', (0, -1.235, 1.575), (0.44, 0.018, 0.050), m_dark, 18, 4)
add_eye_part('Mike_EyeDetail_Left_Eyelid_Rim', (-0.515, -1.235, 1.92), (0.055, 0.018, 0.38), m_dark, 12, 4)
add_eye_part('Mike_EyeDetail_Right_Eyelid_Rim', (0.515, -1.235, 1.92), (0.055, 0.018, 0.38), m_dark, 12, 4)

for obj in bpy.context.scene.objects:
    obj.select_set(False)
body = bpy.data.objects.get('Mike_LowPoly_Round_Body')
if body:
    body.select_set(True)
    bpy.context.view_layer.objects.active = body

print('FIXED: body is bright green, black covering shell removed, detailed layered eye rebuilt')
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