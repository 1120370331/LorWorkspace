"""Author the reserved invention-unit XML ranges without changing player rewards."""
import json
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parent
PACKAGE = ROOT / 'AceTaffyMod'
FIRST_UNIT, FIRST_CARD = 1883400, 1883500
QUALIFIERS = ('奇特的', '诡异的', '脆弱的', '神秘的', '超级的')
HP_MULT = (1, 1.1, .7, .9, 1.3)
BP_MULT = (1, 1, .8, 1.1, 1.25)
ROLES = (
    ('机关装置',150,80,('Normal','Endure','Weak'),('Normal','Endure','Weak'),'每命中3次，下一幕获得1层[强壮]。在场时，其他机关造成的伤害+15%。'),
    ('连锁装置',140,75,('Weak','Normal','Endure'),('Vulnerable','Normal','Normal'),'使用[连锁电弧]后，对距离目标最近的另一名敌方角色再次使用该书页。在场时，其他机关造成的混乱伤害+15%。'),
    ('防御装置',180,100,('Resist','Weak','Endure'),('Endure','Weak','Normal'),'在场时，为其他友方角色提供2层持续[守护]（受到攻击的伤害-2）。自身被击败后失效。'),
    ('毁灭装置',145,70,('Endure','Normal','Weak'),('Weak','Endure','Normal'),'造成的伤害和混乱伤害+20%。在场时，其他机关的“进攻型”骰子最大值+1。'),
    ('能源装置',130,85,('Normal','Weak','Resist'),('Normal','Weak','Endure'),'使用战斗书页时获得2层[烟气]，每一幕结束时恢复5点体力。在场时，其他机关的所有骰子威力+1。'),
)
TRAITS = (
    '每张书页的首颗“进攻型”骰子拼点胜利或单方面攻击命中时，有50%的概率重复投掷1次（重复投掷不会再次触发）。',
    '每张书页首次命中时，下一幕对目标施加1层[虚弱]。',
    '每张书页首次命中时，下一幕自身与目标各获得1层[易损]。',
    '每张书页首次命中时，下一幕自身与目标各随机获得1层[强壮]、[忍耐]、[迅捷]、[守护]、[虚弱]、[破绽]、[束缚]或[易损]。',
    '本机关的类型效果、对友方的支援效果及书页施加的状态层数翻倍；不影响骰子基础点数、书页扣血及塔菲的增益层数。',
)
AURA_NAMES = ('偏心调速','诡异干扰','脆弱增幅','神秘校准','超级驱动')
AURAS = ('速度+1。','每张书页首次命中时，下一幕对目标施加1层[虚弱]。','造成的伤害和混乱伤害+15%。','“防御型”骰子威力+1。','“进攻型”骰子威力+1。')
RANK = ('Weak','Vulnerable','Normal','Endure','Resist')

def resistances(role, qualifier):
    hp,bp=list(role[3]),list(role[4])
    def shift(value,n): return RANK[max(0,min(4,RANK.index(value)+n))]
    if qualifier==1: hp[1]=shift(hp[1],1);bp[0]=shift(bp[0],-1)
    elif qualifier==2: hp=[shift(v,-1) for v in hp];bp=[shift(v,-1) for v in bp]
    elif qualifier==3: hp=hp[1:]+hp[:1];bp=bp[1:]+bp[:1]
    elif qualifier==4: hp=[shift(v,1) for v in hp]
    return hp,bp

def edit(relative, callback):
    path=PACKAGE/relative;tree=ET.parse(path);callback(tree.getroot());ET.indent(tree)
    tree.write(path,encoding='utf-8',xml_declaration=True)

def remove_ids(root, tag, first, last):
    for node in list(root.findall(tag)):
        if first<=int(node.get('ID','-1'))<=last:root.remove(node)

def field(node,key,value): ET.SubElement(node,key).text=str(value)

def make_units():
    result=[]
    for q,qualifier in enumerate(QUALIFIERS):
        for k,role in enumerate(ROLES):
            hp,bp=resistances(role,q)
            result.append({'id':FIRST_UNIT+q*5+k,'qualifier':q,'kind':k,'name':qualifier+role[0],
                           'hp':int(int(role[1]*HP_MULT[q]+.5)*.8+.5),'break':int(int(role[2]*BP_MULT[q]+.5)*.8+.5),
                           'hp_resists':hp,'break_resists':bp,'skin':f'AceTaffy_Invention_{q}_{k}',
                           'cards':[FIRST_CARD+2*k,FIRST_CARD+2*k+1]})
    return result

def write_books(root,units):
    remove_ids(root,'Book',FIRST_UNIT,FIRST_UNIT+24)
    boss=root.find("Book[@ID='1883002']/EquipEffect")
    if not any(x.text=='1883206' for x in boss.findall('Passive')):field(boss,'Passive',1883206)
    for unit in units:
        book=ET.SubElement(root,'Book',ID=str(unit['id']))
        for k,v in [('Name',unit['name']),('TextId',-1),('Category','DeckFixed'),('Option','Basic'),('SpeedDiceNum',2)]:field(book,k,v)
        effect=ET.SubElement(book,'EquipEffect')
        for k,v in [('HP',unit['hp']),('Break',unit['break']),('SpeedMin',2),('Speed',4),('StartPlayPoint',2),('MaxPlayPoint',2)]:field(effect,k,v)
        for tag,value in zip(('SResist','PResist','HResist','SBResist','PBResist','HBResist'),unit['hp_resists']+unit['break_resists']):field(effect,tag,value)
        field(effect,'Passive',1883205)
        if unit['kind']==4:ET.SubElement(effect,'Passive',Pid='@origin').text='240026'
        for k,v in [('Chapter',6),('Episode',1),('Rarity','Unique'),('CharacterSkin','Custom_'+unit['skin']),('CharacterSkinType','Custom')]:field(book,k,v)

def write_enemies(root,units):
    remove_ids(root,'Enemy',FIRST_UNIT,FIRST_UNIT+24)
    for unit in units:
        node=ET.SubElement(root,'Enemy',ID=str(unit['id']))
        for k,v in [('Name',unit['name']),('FaceType','Skin'),('NameID',-1),('MinHeight',170),('MaxHeight',170),('Unknown','false'),('Gender','F'),('Retreat','false'),('BookId',unit['id']),('BodyId',0),('Exp',0),('DropBonus',0),('AiScript','')]:field(node,k,v)

def write_decks(root,units):
    remove_ids(root,'Deck',FIRST_UNIT,FIRST_UNIT+24)
    for unit in units:
        node=ET.SubElement(root,'Deck',ID=str(unit['id']))
        for card in unit['cards']:field(node,'Card',card)

# (minimum, maximum, type, detail, dice script). Each type owns two pages.
CARDS = (
    ('棘轮连打','','',[(3,7,'Atk','Hit','Bleed'),(3,7,'Atk','Hit','Bleed')]),
    ('回转重锤','','',[(3,6,'Def','Guard',''),(5,9,'Atk','Hit','Disarm'),(2,5,'Standby','Hit','')]),
    ('拖曳锁链','','',[(3,7,'Atk','Penetrate','Bind'),(3,6,'Atk','Penetrate','Bind')]),
    ('连锁电弧','使用后，对距离目标最近的另一名敌方角色再次使用本书页；超级机关改为额外选择2名目标。','AceTaffy_DeviceChain',[(4,7,'Atk','Penetrate','Paralyze'),(3,6,'Atk','Penetrate','Weak')]),
    ('展开壁垒','','',[(5,9,'Def','Guard',''),(4,8,'Def','Guard',''),(3,6,'Atk','Hit','Weak')]),
    ('盾面镇压','','',[(4,8,'Atk','Hit','Disarm'),(4,8,'Def','Guard',''),(2,5,'Standby','Guard','')]),
    ('破甲爆裂','','',[(5,9,'Atk','Hit','Vulnerable'),(4,8,'Atk','Penetrate','Bleed')]),
    ('过载冲撞','[使用时]失去5点体力。','AceTaffy_DeviceOverload',[(3,6,'Def','Guard',''),(7,11,'Atk','Hit','Weak')]),
    ('废汽虹吸','','',[(3,7,'Def','Evasion',''),(3,6,'Atk','Penetrate','Smoke'),(3,6,'Def','Guard','')]),
    ('增压喷流','','',[(4,8,'Atk','Penetrate','Smoke'),(3,7,'Atk','Penetrate','Bind')]),
)
DEBUFF_NAMES={'Bleed':'流血','Disarm':'破绽','Bind':'束缚','Paralyze':'麻痹','Weak':'虚弱','Vulnerable':'易损','Smoke':'烟气'}

def write_cards(root):
    remove_ids(root,'Card',FIRST_CARD,FIRST_CARD+9)
    for i,(name,desc,script,dice) in enumerate(CARDS):
        card=ET.SubElement(root,'Card',ID=str(FIRST_CARD+i))
        for k,v in [('Name',name),('Option','NoInventory'),('TextId',-1),('Artwork',f'AceTaffy_Device_{FIRST_CARD+i}.png'),('Rarity','Unique')]:field(card,k,v)
        ET.SubElement(card,'Spec',Range='Far' if i in (3,6,9) else 'Near',Cost='0')
        if script:field(card,'Script',script)
        if desc:field(card,'ScriptDesc',desc)
        behaviours=ET.SubElement(card,'BehaviourList')
        for minimum,maximum,kind,detail,ability in dice:
            motion={'Hit':'H','Penetrate':'Z','Guard':'G','Evasion':'E'}[detail]
            fx={'Hit':'AceTaffy_SteamBlunt','Penetrate':'AceTaffy_SteamPierce','Guard':'AceTaffy_SteamGuard','Evasion':''}[detail]
            text=('[命中时]' + ('' if ability=='Smoke' else '下一幕') + f'对目标施加{2 if ability=="Smoke" else 1}层[{DEBUFF_NAMES[ability]}]。') if ability else ''
            ET.SubElement(behaviours,'Behaviour',Min=str(minimum),Dice=str(maximum),Type=kind,Detail=detail,Motion=motion,EffectRes=fx,
                          Script='AceTaffy_Device'+ability if ability else '',ActionScript='',Desc=text)
        ET.SubElement(card,'AbilityList');field(card,'Chapter',6);field(card,'Priority',0)

def write_card_names(root):
    ls=root.find('cardDescList');remove_ids(ls,'BattleCardDesc',FIRST_CARD,FIRST_CARD+9)
    for i,(name,desc,_,__) in enumerate(CARDS):
        card=ET.SubElement(ls,'BattleCardDesc',ID=str(FIRST_CARD+i));field(card,'LocalizedName',name);field(card,'Ability',desc)

PASSIVES={
1883201:('速战速决Ⅵ','速度骰子槽数量+5。情感等级达到3级后额外+1。'),
1883202:('蒸汽工坊','舞台开始时召唤蒸汽工坊。工坊存活时，每一幕开始时获得1层[发明进度]；工坊每被命中一次，自身恢复10点混乱抗性并获得1层[发明进度]。进度达到5层时清空，于下一幕开始时召唤1个随机机关（在场与待登场机关合计至多4个）。名额已满时，改为使所有友方角色下一幕各随机获得3层[强壮]、[迅捷]或[守护]。工坊先于自身被击败时，永久摧毁自身2颗速度骰子，取消待登场机关，且不再发明。自身被击败时，所有召唤物退场。'),
1883203:('变幻莫测','每一幕使用的书页将发生变化。优先将2张[临场发明]配备至速度最低的2颗速度骰子，其余书页从可支付光芒费用的书页中随机选择。蒸汽工坊被击败后不再使用[临场发明]。'),
1883204:('蒸汽工坊','无法行动，免疫混乱伤害。每被命中一次，使永雏塔菲恢复10点混乱抗性并获得1层[发明进度]。先于永雏塔菲被击败时，永久摧毁其2颗速度骰子，并使其停止发明。'),
1883205:('自律发明','拥有2颗速度骰子，不受情感等级影响。每一幕使用2张专属书页。登场时使塔菲获得1层对应增益，之后每一幕开始时再增加1层；每个机关的增益独立累积，自身被击败时移除自身提供的全部层数。类型与词条效果详见状态说明。'),
1883206:('八重庇护','舞台开始时获得8层持续[守护]。每有1个己方机关被击败，层数-1；层数归零后，每有1个己方机关被击败，便获得1层持续[易损]。蒸汽工坊被击败及塔菲被击败后的召唤物退场不会触发此效果。')}

def write_passives(root,tag):
    for id,(name,desc) in PASSIVES.items():
        node=root.find(f"{tag}[@ID='{id}']")
        if node is None:
            node=ET.SubElement(root,tag,ID=str(id))
            if tag=='Passive':
                for k,v in [('Negative','false'),('IsHide','false'),('Rarity','Unique'),('Lock','false'),('CanGivePassive','false'),('CanReceivePassive','false'),('InnerType',-1),('Cost',0)]:field(node,k,v)
        for k,v in [('Name',name),('Desc',desc)]:
            child=node.find(k)
            if child is None:field(node,k,v)
            else:child.text=v

def write_effects(root,units):
    ls=root.find('effectTextList')
    old=ls.find("BattleEffectText[@ID='AceTaffy_PendingInventions']")
    if old is not None: ls.remove(old)
    values={
        'AceTaffy_ScheduledInventions':('待登场机关','下一幕开始时召唤{0}个已完成的机关。占用机关名额，尚未提供增益。蒸汽工坊或塔菲被击败时取消。'),
        'AceTaffy_InventionProgress':('发明进度','蒸汽工坊存活时，每一幕开始时获得1层；工坊每被命中一次再获得1层。达到5层时清空，于下一幕开始时召唤1个随机机关（在场与待登场机关合计至多4个）。名额已满时，改为使所有友方角色下一幕各随机获得3层[强壮]、[迅捷]或[守护]。'),
        'AceTaffy_ManufactureGuard':('八重守护','被击中时所受伤害-{0}。这一效果不会在幕结束时消失。每有1个己方机关被击败，层数-1。'),
        'AceTaffy_ManufactureVulnerable':('发明易损','被击中时所受伤害+{0}。这一效果不会在幕结束时消失。已有8个己方机关被击败后，每有1个己方机关被击败，层数+1。'),
        'AceTaffy_DeviceSupport':('机关支援','每个在场的其他防御装置提供2层持续[守护]。若自身是机关，还会获得其他机关的支援：机关装置使伤害+15%，连锁装置使混乱伤害+15%，毁灭装置使“进攻型”骰子最大值+1，能源装置使所有骰子威力+1。超级装置的支援效果翻倍。同类支援可叠加，来源被击败后失效。')}
    for key,name in [('Strength','强壮'),('Quickness','迅捷'),('Protection','守护')]:
        values['AceTaffy_Overdrive_'+key]=('准备·'+name,'下一幕获得{0}层['+name+']。')
    for q in range(5):values[f'AceTaffy_InventionAura_{q}']=(AURA_NAMES[q],'每层效果：'+AURAS[q]+'每个'+QUALIFIERS[q]+'机关登场时及每一幕开始时，层数+1；该机关被击败时，其提供的全部层数一并移除。不同机关的层数独立计算。')
    for u in units:
        q,k=u['qualifier'],u['kind']
        values[f'AceTaffy_Invention_{q}_{k}']=(u['name'],ROLES[k][5]+TRAITS[q]+'登场时及每一幕开始时，使塔菲获得1层['+AURA_NAMES[q]+']，每层效果：'+AURAS[q]+'自身被击败时，移除自身提供的全部增益层数。')
    for key,(name,desc) in values.items():
        node=ls.find(f"BattleEffectText[@ID='{key}']")
        if node is None:node=ET.SubElement(ls,'BattleEffectText',ID=key)
        for k,v in [('Name',name),('Desc',desc)]:
            child=node.find(k)
            if child is None:field(node,k,v)
            else:child.text=v

def write_abilities(root):
    descriptions={
        'AceTaffy_Improvise':'以蒸汽工坊为目标，可被敌方角色转移攻击目标。拼点结束后，未被击败或摧毁的骰子将继续攻击蒸汽工坊1次。工坊被击败后取消本书页。',
        'AceTaffy_DeviceChain':CARDS[3][1],
        'AceTaffy_DeviceOverload':CARDS[7][1],
    }
    for key,name in DEBUFF_NAMES.items():
        descriptions['AceTaffy_Device'+key]='[命中时]'+('' if key=='Smoke' else '下一幕')+f'对目标施加{2 if key=="Smoke" else 1}层[{name}]。'
    for key,desc in descriptions.items():
        node=root.find(f"BattleCardAbility[@ID='{key}']")
        if node is None:node=ET.SubElement(root,'BattleCardAbility',ID=key)
        child=node.find('Desc')
        if child is None:field(node,'Desc',desc)
        else:child.text=desc


def main():
    units=make_units()
    edit('Data/EquipPage_Enemy.xml',lambda r:write_books(r,units))
    edit('Data/EnemyUnitInfo.xml',lambda r:write_enemies(r,units))
    edit('Data/Deck_Enemy.xml',lambda r:write_decks(r,units))
    edit('Data/CardInfo.xml',write_cards)
    edit('Localize/cn/BattleCardAbilities/BattleCardAbilities_AceTaffy.xml',write_abilities)
    edit('Localize/cn/BattlesCards/BattleCardDesc_AceTaffy.xml',write_card_names)
    edit('Data/PassiveList.xml',lambda r:write_passives(r,'Passive'))
    edit('Localize/cn/PassiveDesc/PassiveDesc.xml',lambda r:write_passives(r,'PassiveDesc'))
    edit('Localize/cn/EffectTexts/AceTaffy_Effects.xml',lambda r:write_effects(r,units))
    dest=ROOT/'ArtSource/Character/invention-units'
    dest.mkdir(parents=True,exist_ok=True)
    (dest/'unit-manifest.json').write_text(json.dumps({'units':units},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('Authored 25 invention units and 10 enemy-only pages')

if __name__=='__main__':main()
