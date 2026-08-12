const net = require('net');
const path = require('path');

const outputPath = path.resolve(__dirname, '..', 'Art', 'kaydetbutton.png').replace(/\\/g, '/');

const code = String.raw`
import bpy, math

output_path = r'${outputPath}'

# Clear only previous icon objects/lights created by this script.
for obj in list(bpy.context.scene.objects):
    if obj.name.startswith('SaveIcon_'):
        bpy.data.objects.remove(obj, do_unlink=True)

def toon_mat(name, color):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    toon = nodes.new('ShaderNodeBsdfToon')
    toon.inputs['Color'].default_value = color
    toon.inputs['Size'].default_value = 0.62
    toon.inputs['Smooth'].default_value = 0.045
    mat.node_tree.links.new(toon.outputs['BSDF'], out.inputs['Surface'])
    return mat

blue = toon_mat('SaveIcon_Toon_Royal_Blue', (0.05, 0.30, 0.88, 1))
blue_dark = toon_mat('SaveIcon_Toon_Dark_Blue_Rim', (0.02, 0.08, 0.36, 1))
blue_light = toon_mat('SaveIcon_Toon_Sky_Highlight', (0.25, 0.62, 1.0, 1))
white = toon_mat('SaveIcon_Toon_White', (0.96, 0.98, 1.0, 1))
cream = toon_mat('SaveIcon_Toon_Cream', (0.96, 0.88, 0.66, 1))
ink = toon_mat('SaveIcon_Toon_Dark_Line', (0.015, 0.018, 0.035, 1))

col = bpy.data.collections.get('SaveIcon_SaveButton') or bpy.data.collections.new('SaveIcon_SaveButton')
if col.name not in bpy.context.scene.collection.children:
    try:
        bpy.context.scene.collection.children.link(col)
    except Exception:
        pass

def link(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj

def cyl(name, loc, radius, depth, mat, vertices=48, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    link(obj)
    try: bpy.ops.object.shade_flat()
    except Exception: pass
    return obj

def cube(name, loc, scale, mat, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    link(obj)
    try: bpy.ops.object.shade_flat()
    except Exception: pass
    return obj

# Icon faces camera on -Y, with thickness on Y axis.
rot_face = (math.radians(90), 0, 0)
cyl('SaveIcon_Round_Button_Dark_Outer_Rim', (0, 0.06, 0), 1.38, 0.20, blue_dark, 64, rot_face)
cyl('SaveIcon_Round_Button_Main_Blue', (0, -0.02, 0), 1.22, 0.22, blue, 64, rot_face)
cyl('SaveIcon_Round_Button_Inner_Highlight', (-0.22, -0.145, 0.28), 0.70, 0.035, blue_light, 40, rot_face)

# Floppy/save glyph, slightly raised from button face.
cube('SaveIcon_Floppy_Main_White', (0, -0.28, -0.08), (0.62, 0.035, 0.62), white)
cube('SaveIcon_Floppy_Top_Dark_Label', (0.08, -0.325, 0.31), (0.36, 0.020, 0.16), ink)
cube('SaveIcon_Floppy_Metal_Notch', (-0.36, -0.335, 0.31), (0.13, 0.022, 0.16), cream)
cube('SaveIcon_Floppy_Bottom_Label', (0, -0.335, -0.33), (0.42, 0.024, 0.18), cream)
cube('SaveIcon_Floppy_Label_Line_1', (0, -0.36, -0.30), (0.28, 0.012, 0.018), ink)
cube('SaveIcon_Floppy_Label_Line_2', (0, -0.36, -0.38), (0.22, 0.012, 0.016), ink)

# Small cartoon shine dot.
cyl('SaveIcon_Top_Left_Sparkle', (-0.55, -0.36, 0.72), 0.09, 0.03, white, 16, rot_face)

# Camera/light/render settings.
for obj in list(bpy.context.scene.objects):
    if obj.name.startswith('SaveIcon_Light'):
        bpy.data.objects.remove(obj, do_unlink=True)

bpy.ops.object.light_add(type='AREA', location=(0, -3.5, 3.0))
key = bpy.context.object
key.name = 'SaveIcon_Light_Key'
key.data.energy = 420
key.data.size = 4.0

cam = bpy.data.objects.get('SaveIcon_Camera')
if not cam:
    bpy.ops.object.camera_add()
    cam = bpy.context.object
    cam.name = 'SaveIcon_Camera'
cam.location = (0, -5.2, 0)
cam.rotation_euler = (math.radians(90), 0, 0)
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 3.15
bpy.context.scene.camera = cam

try:
    engines = [i.identifier for i in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    bpy.context.scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
except Exception:
    pass

bpy.context.scene.render.resolution_x = 512
bpy.context.scene.render.resolution_y = 512
bpy.context.scene.render.film_transparent = True
bpy.context.scene.render.image_settings.file_format = 'PNG'
bpy.context.scene.render.image_settings.color_mode = 'RGBA'
bpy.context.scene.render.filepath = output_path
bpy.context.scene.view_settings.view_transform = 'Standard'
bpy.context.scene.view_settings.look = 'Medium High Contrast'
bpy.context.scene.render.use_freestyle = True

# Select generated icon pieces.
for obj in bpy.context.scene.objects:
    obj.select_set(False)
for obj in col.objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects.get('SaveIcon_Round_Button_Main_Blue')

bpy.ops.render.render(write_still=True)
print('DONE: save button icon rendered to', output_path)
`;

const payload = JSON.stringify({ type: 'execute_code', params: { code } });
const c = net.createConnection(9876, '127.0.0.1', () => c.write(payload));
let d = '';
c.setTimeout(180000);
c.on('data', x => { d += x; try { JSON.parse(d); console.log(d); c.end(); } catch(e) {} });
c.on('timeout', () => { console.log(d || 'TIMEOUT'); c.destroy(); process.exit(1); });
c.on('error', e => { console.error(e); process.exit(1); });