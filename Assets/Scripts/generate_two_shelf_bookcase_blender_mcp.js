const net = require('net');

const code = String.raw`
import bpy, math

for cn in ['LowPoly_Cartoon_Book','LowPoly_Two_Shelf_Bookcase']:
    c=bpy.data.collections.get(cn)
    if c:
        for o in list(c.objects): bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.collections.remove(c)
for o in list(bpy.context.scene.objects):
    if o.name.startswith('Book_') or o.name.startswith('Bookcase_'):
        bpy.data.objects.remove(o, do_unlink=True)

def mat(n, color):
    m=bpy.data.materials.get(n) or bpy.data.materials.new(n); m.diffuse_color=color; m.use_nodes=True
    ns=m.node_tree.nodes; ns.clear(); out=ns.new('ShaderNodeOutputMaterial'); toon=ns.new('ShaderNodeBsdfToon')
    toon.inputs['Color'].default_value=color; toon.inputs['Size'].default_value=.55; toon.inputs['Smooth'].default_value=.04
    m.node_tree.links.new(toon.outputs['BSDF'], out.inputs['Surface']); return m

wood=mat('Bookcase_Toon_Warm_Wood',(0.55,0.30,0.12,1)); dark=mat('Bookcase_Toon_Dark_Wood',(0.25,0.12,0.05,1)); light=mat('Bookcase_Toon_Wood_Highlight',(0.82,0.52,0.24,1))
cream=mat('Bookcase_Toon_Cream_Pages',(0.96,0.88,0.66,1)); gold=mat('Bookcase_Toon_Gold_Details',(1,.70,.12,1)); line=mat('Bookcase_Toon_Dark_Line',(.015,.012,.01,1)); shadow=mat('Bookcase_Toon_Soft_Shadow',(.035,.025,.02,1))
bookm=[mat('Bookcase_Book_Red',(.80,.08,.08,1)),mat('Bookcase_Book_Blue',(.04,.24,.78,1)),mat('Bookcase_Book_Green',(.10,.58,.20,1)),mat('Bookcase_Book_Purple',(.45,.16,.72,1)),mat('Bookcase_Book_Orange',(.95,.38,.06,1)),mat('Bookcase_Book_Teal',(0,.62,.62,1)),mat('Bookcase_Book_Yellow',(.95,.78,.08,1))]

col=bpy.data.collections.new('LowPoly_Two_Shelf_Bookcase'); bpy.context.scene.collection.children.link(col)
def link(o):
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o); return o
def cube(n, loc, scale, ma, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot); o=bpy.context.object; o.name=n; o.scale=scale; o.data.materials.append(ma); link(o); bpy.ops.object.shade_flat(); return o

# two-shelf frame
cube('Bookcase_Back_Panel_Dark_Wood',(0,.18,1.35),(2.75,.10,1.55),dark)
cube('Bookcase_Left_Side_Wood',(-2.85,0,1.35),(.16,.42,1.75),wood); cube('Bookcase_Right_Side_Wood',(2.85,0,1.35),(.16,.42,1.75),wood)
cube('Bookcase_Top_Board_Wood',(0,0,3.10),(3.02,.46,.16),wood); cube('Bookcase_Middle_Shelf_Wood',(0,0,1.62),(3.02,.46,.14),wood); cube('Bookcase_Bottom_Shelf_Wood',(0,0,.10),(3.02,.46,.16),wood)
cube('Bookcase_Top_Highlight',(0,-.245,3.20),(2.8,.035,.035),light); cube('Bookcase_Middle_Highlight',(0,-.245,1.72),(2.8,.035,.03),light); cube('Bookcase_Bottom_Highlight',(0,-.245,.20),(2.8,.035,.035),light)

specs=[(-1.85,.34,1.05,0,-3),(-1.38,.42,1.22,1,2),(-.82,.30,.96,2,-2),(-.36,.48,1.15,3,1),(.25,.36,1.02,4,-1),(-1.72,.38,.98,5,2),(-1.20,.32,1.18,6,-2),(-.74,.44,1.08,0,1),(-.16,.34,1.25,2,-3),(.35,.46,1.00,1,2)]
def book(i,x,w,h,mi,tilt,shelfz):
    z=shelfz+h/2+.08; r=(0,0,math.radians(tilt)); ma=bookm[mi%len(bookm)]
    cube(f'Bookcase_Book_{i:02d}_Cover',(x,-.11,z),(w/2,.18,h/2),ma,r)
    cube(f'Bookcase_Book_{i:02d}_Pages',(x+w/2-.035,-.315,z),(.035,.035,h/2-.05),cream,r)
    cube(f'Bookcase_Book_{i:02d}_Gold_Band_Top',(x,-.315,z+h*.26),(w/2-.035,.025,.025),gold,r)
    cube(f'Bookcase_Book_{i:02d}_Gold_Band_Bottom',(x,-.315,z-h*.24),(w/2-.035,.025,.025),gold,r)
    cube(f'Bookcase_Book_{i:02d}_Title_Line',(x,-.322,z),(w/3,.018,.020),line,r)
for i,s in enumerate(specs,1): book(i,*s, .18 if i<=5 else 1.70)

cube('Bookcase_Left_Foot',(-2.25,.02,-.10),(.36,.36,.15),dark); cube('Bookcase_Right_Foot',(2.25,.02,-.10),(.36,.36,.15),dark); cube('Bookcase_Cartoon_Ground_Shadow',(0,.08,-.23),(3.25,.72,.035),shadow)

for o in list(bpy.context.scene.objects): o.select_set(o.type=='LIGHT')
bpy.ops.object.delete(); bpy.ops.object.light_add(type='AREA', location=(0,-5,5)); key=bpy.context.object; key.name='Bookcase_Toon_Key_Area_Light'; key.data.energy=520; key.data.size=5.5
bpy.ops.object.light_add(type='POINT', location=(-4,2,3)); fill=bpy.context.object; fill.name='Bookcase_Toon_Fill_Light'; fill.data.energy=105
cam=bpy.data.objects.get('Camera')
if not cam: bpy.ops.object.camera_add(); cam=bpy.context.object
cam.location=(3.9,-5.6,2.7); cam.rotation_euler=(math.radians(64),0,math.radians(36)); bpy.context.scene.camera=cam
try:
    engines=[i.identifier for i in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items]; bpy.context.scene.render.engine='BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_EEVEE'
except Exception: pass
bpy.context.scene.view_settings.view_transform='Standard'; bpy.context.scene.view_settings.look='Medium High Contrast'; bpy.context.scene.render.use_freestyle=True
for o in bpy.context.scene.objects: o.select_set(False)
for o in col.objects: o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects.get('Bookcase_Back_Panel_Dark_Wood')
print('DONE: two-shelf low-poly cartoon bookcase generated with 10 books. Objects:', len(col.objects))
`;

const payload = JSON.stringify({ type: 'execute_code', params: { code } });
const c = net.createConnection(9876, '127.0.0.1', () => c.write(payload));
let d = '';
c.setTimeout(180000);
c.on('data', x => { d += x; try { JSON.parse(d); console.log(d); c.end(); } catch(e) {} });
c.on('timeout', () => { console.log(d || 'TIMEOUT'); c.destroy(); process.exit(1); });
c.on('error', e => { console.error(e); process.exit(1); });