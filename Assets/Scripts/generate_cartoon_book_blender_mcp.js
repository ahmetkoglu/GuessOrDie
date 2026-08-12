const net = require('net');

const code = String.raw`
import bpy, math

# ---------- Cleanup: remove Mike Wazowski objects/collections ----------
for col_name in ['LowPoly_Toon_Mike_Wazowski']:
    col = bpy.data.collections.get(col_name)
    if col:
        for obj in list(col.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(col)

for obj in list(bpy.context.scene.objects):
    if obj.name.startswith('Mike_') or obj.name.startswith('Mike.'):
        bpy.data.objects.remove(obj, do_unlink=True)

# ---------- Materials ----------
def toon_mat(name, color, size=0.55, smooth=0.04):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    toon = nodes.new('ShaderNodeBsdfToon')
    toon.inputs['Color'].default_value = color
    toon.inputs['Size'].default_value = size
    toon.inputs['Smooth'].default_value = smooth
    mat.node_tree.links.new(toon.outputs['BSDF'], out.inputs['Surface'])
    return mat

cover = toon_mat('Book_Toon_Rich_Blue_Cover', (0.05, 0.22, 0.72, 1))
cover_dark = toon_mat('Book_Toon_Dark_Blue_Spine_Shadow', (0.02, 0.08, 0.32, 1))
cover_light = toon_mat('Book_Toon_Sky_Blue_Highlight', (0.20, 0.52, 1.00, 1))
pages = toon_mat('Book_Toon_Warm_Cream_Pages', (0.96, 0.88, 0.65, 1))
page_line = toon_mat('Book_Toon_Page_Line_Tan', (0.62, 0.47, 0.28, 1))
gold = toon_mat('Book_Toon_Gold_Decoration', (1.0, 0.70, 0.12, 1))
ink = toon_mat('Book_Toon_Ink_Dark', (0.015, 0.012, 0.02, 1))

book_col = bpy.data.collections.new('LowPoly_Cartoon_Book')
bpy.context.scene.collection.children.link(book_col)

def link(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    book_col.objects.link(obj)
    return obj

def cube(name, loc, scale, mat, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mat)
    link(obj)
    bpy.ops.object.shade_flat()
    return obj

def cyl(name, loc, radius, depth, mat, vertices=6, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mat)
    link(obj)
    bpy.ops.object.shade_flat()
    return obj

# ---------- Low-poly cartoon closed book ----------
angle = math.radians(-8)
book_rot = (0, 0, angle)

# page block visible under the cover
cube('Book_Page_Block_Cream', (0.08, 0.00, 0.22), (1.85, 1.22, 0.22), pages, book_rot)

# cover pieces, slightly larger than pages
cube('Book_Bottom_Cover_Blue', (0, 0, 0.08), (2.05, 1.38, 0.10), cover_dark, book_rot)
cube('Book_Top_Cover_Blue', (0, 0, 0.43), (2.12, 1.45, 0.10), cover, book_rot)

# spine on the left side
cube('Book_Rounded_Spine_Dark_Blue', (-1.12, 0, 0.29), (0.18, 1.50, 0.45), cover_dark, book_rot)
cyl('Book_LowPoly_Spine_Round_Edge', (-1.22, 0, 0.29), 0.23, 1.52, cover_dark, 8, (math.radians(90), 0, angle))

# cover highlight and gold decoration
cube('Book_Cover_Cartoon_Highlight', (0.20, -0.42, 0.495), (1.18, 0.055, 0.012), cover_light, book_rot)
cube('Book_Gold_Title_Plate', (0.28, 0.00, 0.505), (0.78, 0.34, 0.018), gold, book_rot)
cube('Book_Gold_Title_Line_1', (0.28, -0.055, 0.525), (0.50, 0.025, 0.014), ink, book_rot)
cube('Book_Gold_Title_Line_2', (0.28, 0.050, 0.525), (0.42, 0.022, 0.014), ink, book_rot)

# gold bands on spine
for i, y in enumerate([-0.48, 0.0, 0.48]):
    cube(f'Book_Gold_Spine_Band_{i+1}', (-1.23, y, 0.53), (0.045, 0.18, 0.025), gold, book_rot)

# page lines on the right side
for i, z in enumerate([0.16, 0.22, 0.28, 0.34]):
    cube(f'Book_Page_Line_Right_{i+1}', (1.02, 0, z), (0.025, 1.10, 0.010), page_line, book_rot)

# page lines at the front/bottom side
for i, x in enumerate([-0.55, -0.15, 0.25, 0.65]):
    cube(f'Book_Page_Line_Front_{i+1}', (x, -0.68, 0.30), (0.22, 0.018, 0.010), page_line, book_rot)

# small bookmark ribbon
cube('Book_Red_Bookmark_Ribbon', (0.62, 0.73, 0.13), (0.12, 0.34, 0.035), toon_mat('Book_Toon_Red_Bookmark', (0.85, 0.04, 0.04, 1)), book_rot)

# black cartoon base/shadow, separate from the model (not covering it)
cube('Book_Soft_Cartoon_Ground_Shadow', (0.08, 0.02, -0.035), (2.25, 1.55, 0.025), toon_mat('Book_Toon_Soft_Shadow', (0.03, 0.025, 0.035, 1)), book_rot)

# ---------- Presentation ----------
for obj in list(bpy.context.scene.objects):
    if obj.type == 'LIGHT':
        obj.select_set(True)
    else:
        obj.select_set(False)
bpy.ops.object.delete()

bpy.ops.object.light_add(type='AREA', location=(0, -4, 4.2))
key = bpy.context.object
key.name = 'Book_Toon_Key_Area_Light'
key.data.energy = 430
key.data.size = 5

bpy.ops.object.light_add(type='POINT', location=(-3, 2, 2.5))
fill = bpy.context.object
fill.name = 'Book_Toon_Warm_Fill_Light'
fill.data.energy = 90

cam = bpy.data.objects.get('Camera')
if not cam:
    bpy.ops.object.camera_add()
    cam = bpy.context.object
cam.location = (3.1, -4.1, 2.2)
cam.rotation_euler = (math.radians(62), 0, math.radians(38))
bpy.context.scene.camera = cam

try:
    engines = [i.identifier for i in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]
    bpy.context.scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
except Exception:
    pass
bpy.context.scene.view_settings.view_transform = 'Standard'
bpy.context.scene.view_settings.look = 'Medium High Contrast'
bpy.context.scene.render.use_freestyle = True

for obj in bpy.context.scene.objects:
    obj.select_set(False)
for obj in book_col.objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects.get('Book_Top_Cover_Blue')

print('DONE: Mike removed. Low-poly cartoon book generated with toon colors. Objects:', len(book_col.objects))
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