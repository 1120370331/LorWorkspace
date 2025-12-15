# Assembly-CSharp.dll 探索笔记 (基于部分反编译结构)

这份笔记旨在帮助你开始探索《废墟图书馆》的游戏代码，以便进行 Mod 开发。信息主要基于提供的文件/文件夹列表和常见的 Mod 开发目标。

## 关键区域与概念

以下是一些你在 Mod 开发中可能会经常接触到的关键区域和相关类/概念：
游戏和开发框架的原代码已经被反编译并存放在/Assembly-CSharp 和/BaseMod中
1.  **核心战斗与角色 (`BattleUnitModel`, `UnitDataModel`, 等):**
    *   **`BattleUnitModel.cs` (未直接列出，但极其重要)**: 代表战场上的一个单位（司书或敌人），包含其状态（HP, Stagger, Emotion Level）、持有的卡牌、被动、Buff 等。这是修改角色行为、属性、添加新机制的核心。
    *   **`UnitDataModel.cs`**: 存储单位的基础数据（如 ID, 皮肤, 专属卡牌等）。
    *   **`PassiveAbility_*.cs`**: 示例被动。查看这些文件可以了解如何实现被动能力 (`PassiveAbilityBase` 通常是基类，也需要查找)。添加自定义被动是常见的 Mod 类型。
    *   **`BattleUnitBuf.cs` (未直接列出，但相关文件如 `BattleUnitBuf_KeterFinal_LibrarianAura.cs` 存在)**: 处理 Buff 和 Debuff 的基类和具体实现。

2.  **卡牌与骰子 (`LOR_DiceSystem`, `DiceCardAbilityBase`, `DiceCardXmlInfo`):**
    *   **`LOR_DiceSystem/`**: 这个文件夹（及其内容）很可能包含了骰子战斗系统的核心逻辑。
    *   **`DiceCardAbilityBase.cs` (未直接列出，但极其重要)**: 所有卡牌效果的基础类。创建自定义卡牌效果需要继承并实现这个类的方法。
    *   **`DiceCardXmlInfo.cs` (未直接列出)**: 通常用于表示从 XML 加载的卡牌数据。

3.  **关卡控制 (`StageController`, `EnemyTeamStageManager_*`):**
    *   **`StageController.cs`**: 管理整个战斗关卡的流程，包括回合开始/结束、敌人阶段、波次管理等。是修改战斗流程、添加自定义事件的重要目标。
    *   **`EnemyTeamStageManager_*.cs`**: 针对特定敌人的特殊关卡逻辑（例如 Boss 战机制）。
    *   **`StageModel.cs`, `StageWaveModel.cs`**: 定义关卡和波次的数据。

4.  **数据加载与定义 (`LOR_XML`, XML 文件):**
    *   **`LOR_XML/`**: 这个文件夹很可能包含了解析游戏数据 XML 文件的代码。
    *   **游戏数据 XML**: 《废墟图书馆》大量使用 XML 文件来定义卡牌、被动、书籍、敌人、关卡等。理解这些 XML 的结构以及游戏如何加载它们 (`XmlUtil` 等相关类) 对于添加新内容至关重要。通常 Mod 也需要提供自己的 XML 文件。

5.  **用户界面 (`UI/`, `UI*.cs`):**
    *   **`UI/`**: 包含各种 UI 元素的实现。
    *   **`UICharacterProtrait.cs`, `UICustomSelectableExtension.cs` 等**: UI 相关类的示例。修改现有 UI 或添加新 Mod 设置界面会涉及这些。

6.  **书籍与掉落 (`DropTable.cs`, `BookModel.cs`):**
    *   **`DropTable.cs`**: 可能与战斗结束后的书籍掉落逻辑相关。
    *   **`BookModel.cs` (未直接列出)**: 代表游戏中的书籍。

7.  **Mod 支持 (`Mod/`, `ModInfoSaver.cs`):**
    *   **`Mod/`**: 可能包含游戏内置的一些 Mod 支持或示例代码。
    *   **`ModInfoSaver.cs`**: 可能与加载或保存 Mod 信息有关。

## 如何继续探索

*   **使用反编译器 (dnSpy/ILSpy)**: 打开 `Assembly-CSharp.dll` 和相关的 UnityEngine DLLs (`UnityEngine.CoreModule.dll` 等)。
*   **搜索**: 针对你想要修改的功能，搜索上述提到的类名或相关关键词。
*   **交叉引用**: 查看一个方法被哪些其他方法调用，或者它调用了哪些方法，以理解代码流程。
*   **查看基类和接口**: 理解继承关系有助于掌握通用逻辑。

**重要提醒:**

*   在开始编写复杂 Mod 之前，请确保已解决 `HarmonyLib` (即 `0Harmony.dll`) 的引用问题，使项目能够成功编译。
*   这份笔记是基于有限信息的初步推断，实际结构和类名可能需要你在反编译器中进一步确认。

## 添加自定义能力 (根据官方示例)

官方示例展示了添加三种主要能力类型的方法，你需要创建新的类并继承相应的基类：

1.  **添加骰子效果 (Ability of Dice)**
    *   **基类**: `DiceCardAbilityBase`
    *   **命名约定 (推荐)**: `DiceCardAbility_{你的能力名称}`
    *   **示例**: 创建一个在攻击成功时对目标造成 10 点伤害的骰子效果。
        ```csharp
        public class DiceCardAbility_test_mydice : DiceCardAbilityBase
        {
            public static string Desc = "[On Hit] Deal 10 damage to target"; // 效果描述

            // 重写 OnSucceedAttack 方法，在攻击成功时触发
            public override void OnSucceedAttack(BattleUnitModel target)
            {
                target.TakeDamage(10); // 对目标调用 TakeDamage 方法
            }
        }
        ```

2.  **添加书页效果 (Ability of Combat Pages)**
    *   **基类**: `DiceCardSelfAbilityBase`
    *   **命名约定 (推荐)**: `DiceCardSelfAbility_{你的能力名称}`
    *   **示例**: 创建一个在使用书页时为使用者恢复 5 点生命值的效果。
        ```csharp
        public class DiceCardSelfAbility_test_my : DiceCardSelfAbilityBase
        {
            public static string Desc = "[On Use] Recover 5 HP"; // 效果描述

            // 重写 OnUseCard 方法，在使用书页时触发
            public override void OnUseCard()
            {
                owner.RecoverHP(5); // 对所有者 (owner) 调用 RecoverHP 方法
            }
        }
        ```

3.  **添加被动能力 (Ability of Passives)**
    *   **基类**: `PassiveAbilityBase`
    *   **命名约定 (推荐)**: `PassiveAbility_{你的能力名称}`
    *   **(示例未完整显示，但结构类似)** 你需要继承 `PassiveAbilityBase` 并重写其提供的方法（例如 `OnRoundStart`, `OnUseCard`, `BeforeTakeDamage` 等）来实现你的被动逻辑。

**关键点:**

*   你需要为你的能力类提供一个唯一的名字。
*   通过重写 (override) 基类提供的虚方法 (virtual methods) 来实现你的逻辑。你需要使用反编译器查看这些基类有哪些可用的方法可以重写。
*   通常需要提供一个 `Desc` 字段来描述能力的效果，供游戏内显示。
*   **XML 定义 (重要)**: 除了编写 C# 代码，你还需要在 XML 文件中定义这些新的能力，并将它们关联到卡牌或角色上。根据分析 (`Passive_Test.xml` 和 `_PassiveDesc.txt`)，这通常涉及 **两种** XML 文件/条目：
    1.  **被动定义 XML (例如 `PassiveXml/Passive_Test.xml`)**:
        *   **目的**: 定义被动的核心属性和行为链接。
        *   **结构**: 通常包含 `<Passive ID="...">` 标签。
        *   **关键标签**: `<PassiveClass>命名空间.类名, 程序集名</PassiveClass>` 用于链接你的 C# 代码。
        *   **其他标签**: 可能包含 `<Cost>`, `<Rarity>`, `<Name>`, `<Desc>` 等基础信息。
        *   **示例**: 
            ```xml
            <Passive ID="MyDLL.Passive.Test">
              <PassiveClass>MyDLL.PassiveAbility_Test, MyDLL</PassiveClass>
              <Name>测试</Name>
              <Desc>每一幕开始时恢复所有光芒</Desc>
              <Cost>1</Cost>
              <Rarity>Common</Rarity>
              ...
            </Passive>
            ```
    2.  **本地化/描述 XML (例如 `Localize/cn/PassiveDesc/_PassiveDesc.txt`)**:
        *   **目的**: 提供被动在特定语言下的 **显示名称** 和 **详细描述**。
        *   **结构**: 通常包含多个 `<PassiveDesc ID="...">` 标签。
        *   **关键标签**: `<Name>` 和 `<Desc>` 用于 UI 显示。
        *   **关联**: 通过与被动定义 XML 中 **相同的 `ID`** 来关联。
        *   **示例**: 
            ```xml
            <PassiveDesc ID="MyDLL.Passive.Test"> 
              <Name>测试</Name>
              <Desc>每一幕开始时恢复所有光芒</Desc>
            </PassiveDesc>
            ```
    *   **注意**: 必须确保两个文件中使用的 `ID` (例如 `MyDLL.Passive.Test`) 完全一致，游戏才能正确地将描述文本关联到被动能力上。

祝你 Mod 开发顺利！

## 标准 Mod 文件夹结构 (推荐)

根据对成熟 Mod (如"寒昼事务所V4.0") 结构的分析和社区常见实践，一个结构良好的 Mod 文件夹 (例如可以直接放入游戏 Mods 目录的文件夹，我们称之为 `YourModName/`) 通常遵循以下布局：

```
YourModName/
├── Assemblies/
│   └── YourModDLL.dll         # 你编译生成的 C# 代码 DLL
│
├── Data/                      # 核心游戏数据定义 XML
│   ├── PassiveList.xml        # 被动定义 (核心属性, 链接 C# 类)
│   ├── CardInfo.xml           # 卡牌定义
│   ├── BookStory.xml          # 书籍定义/故事
│   ├── StageInfo.xml          # 关卡定义
│   ├── EnemyUnitInfo.xml      # 敌人单位定义
│   ├── EquipPage_Librarian.xml # 司书书页装备
│   ├── Dropbook.xml           # 书籍掉落表
│   └── ... (其他数据定义 XML)
│
├── Localize/
│   ├── cn/                    # 中文本地化
│   │   ├── PassiveDesc/       # 被动描述 (e.g., _PassiveDesc.txt or Passive_MyMod.xml)
│   │   ├── CardAbilityText/   # 卡牌能力描述
│   │   ├── BookDesc/          # 书籍描述
│   │   ├── CharacterName/     # 角色名称
│   │   ├── StageName/         # 关卡名称
│   │   ├── EffectTexts/       # 效果文本 (Buff/Debuff等)
│   │   ├── BattleDialogues/   # 战斗对话
│   │   └── StoryText/         # 剧情文本
│   ├── en/                    # 英文本地化 (结构同上)
│   └── kr/                    # 韩文本地化 (结构同上)
│
├── Resource/                  # 非 XML 资源文件
│   ├── AssetBundle/           # Unity 资源包 (.assets, .ab)
│   ├── CombatPageArtwork/     # 卡牌图片 (.png)
│   ├── CustomAudio/           # 自定义音效/音乐 (.wav, .mp3)
│   ├── Stage/                 # 关卡背景/地板图片
│   ├── StoryBgSprite/         # 剧情背景图
│   ├── StoryStanding/         # 剧情立绘
│   └── ... (其他资源类型, 如 MotionSound, ArtWork/buff 等)
│
├── StageModInfo.xml           # Mod 核心信息文件 (或 ModInfo.xml)
│
└── ... (可选的其他文件, 如 preview.jpg, README.md)
```

**关键点:**

*   **`Data/` vs `Resource/`**: `Data/` 主要存放定义游戏对象（卡牌、被动、敌人等）的 XML 文件。`Resource/` 主要存放图片、音频、AssetBundle 等非 XML 资源。
*   **`Localize/`**: 存放所有需要在 UI 中显示的文本，按语言和类型分类。
*   **一致性**: 遵循这种标准结构可以确保游戏和 Mod 加载器（如 BaseMod）能够正确识别和加载你的 Mod 内容。
*   **分离**: 将 C# 代码 (DLL)、核心数据定义 (XML)、本地化文本和资源文件清晰地分离开来，便于管理和维护。
*   **命名**: 文件和文件夹的命名（尤其是 XML 定义和本地化文件夹下的名称）通常需要遵循特定的约定，建议参考游戏原版文件或成功加载的 Mod。
*   **ID 关联**: 确保定义 XML (如 `Data/PassiveList.xml`) 中的 `ID` 与本地化 XML (如 `Localize/cn/PassiveDesc/`) 中的 `ID` 严格匹配。

参考成熟 Mod 的结构是最佳实践。

### 被动 XML 定义的最佳实践

根据对成熟 Mod (如"寒昼事务所V4.0") 的分析，定义新的被动能力时，推荐遵循以下 XML 实践：

1.  **目标文件**: 将新的被动定义添加到 Mod 文件夹下的 **`Data/PassiveList.xml`** 文件中。通常 Mod 会共享这一个文件来定义所有被动。
2.  **ID 格式**: 使用 **唯一的纯数字** 作为 `<Passive>` 标签的 `ID` 属性值 (例如 `<Passive ID="9000001">`)。选择一个不容易与游戏原版或其他 Mod 冲突的数字范围（如 9000000+）。
3.  **C# 链接**: 使用 `<Script>类名</Script>` 标签来链接你的 C# 被动能力类，仅包含类名，**不含命名空间或程序集名称** (例如 `<Script>PassiveAbility_Test</Script>`)。Mod 加载器（如 BaseMod）通常会自动查找匹配的类。
4.  **核心标签**: 确保 `<Passive>` 标签内包含必要的核心信息，如 `<Rarity>`, `<Cost>`, `<Name>`, `<Desc>`, `<Script>`。
5.  **本地化关联**: 在对应的本地化文件中（例如 `Localize/cn/PassiveDesc/PassiveDesc_MyDLL.xml` 或 `_PassiveDesc.txt`），添加一个 `<PassiveDesc>` 条目，其 `ID` 属性值 **必须与 `PassiveList.xml` 中使用的数字 ID 完全一致**。

```xml
<!-- 示例: Data/PassiveList.xml -->
<PassiveXmlRoot ...>
  <Passive ID="9000001">
    <Script>PassiveAbility_Test</Script>
    <Rarity>Common</Rarity>
    <Cost>1</Cost>
    <Name>测试</Name> 
    <Desc>每一幕开始时恢复所有光芒</Desc>
    ...
  </Passive>
</PassiveXmlRoot>

<!-- 示例: Localize/cn/PassiveDesc/PassiveDesc_MyDLL.xml -->
<PassiveDescRoot ...>
  <PassiveDesc ID="9000001">
    <Name>测试</Name>
    <Desc>每一幕开始时恢复所有光芒</Desc>
  </PassiveDesc>
</PassiveDescRoot>
```

**核心教训**: 仔细参考结构良好的 Mod，特别是 XML 文件的组织方式和内部标签格式，是确保 Mod 正常工作的关键。

### 本地化文件结构注意事项

*   **卡牌名称/描述 (`BattlesCards` 目录):** XML 文件根节点为 `<CardDescXmlRoot>`，内部需要嵌套一层 `<cardDescList>` 标签，再包含具体的 `<BattleCardDesc ID="...">` 条目。
*   **骰子行为描述 (`BattleCardAbilities` 目录):** XML 文件根节点为 `<BattleCardAbilityDescRoot>`，内部**直接包含** `<BattleCardAbility ID="...">` 条目，**不需要** 额外的 `<cardDescList>` 嵌套。

## 实现新角色与卡牌 (基于设定.md)

以下是根据 `设定.md` 实现新角色（如安希尔）及其卡牌的推荐 XML 编辑流程：

### 1. 定义核心书页 (EquipPage)

*   **目标文件**: `NormalInvitation/Data/EquipPage_Librarian.xml` (如果是可供司书使用的角色) 或 `NormalInvitation/Data/EquipPage_Enemy.xml` (如果是敌人角色)。
*   **作用**: 定义角色的核心书页，包含其 ID、基础属性（生命、混乱抗性、速度等）、携带的被动能力列表、外观皮肤、装备限制等。
*   **操作**: 
    *   添加一个新的 `<EquipPage>` 节点。
    *   为其分配一个**唯一的 ID** (格式通常是 `Mod包ID.角色名`，例如 `MyDLL.Anxier`)。
    *   配置基础属性，如 `<Name>`, `<NickName>`, `<Sephirah>`, `<SuccessionPossible>`, `<hp>`, `<breakEndurance>`, `<speedMin>`, `<speed>`, `<emotionLimit>` 等。
    *   在 `<PassiveList>` 子节点中，添加角色拥有的所有被动能力的 **数字 ID** (这些 ID 将在 `PassiveList.xml` 中定义)。
    *   配置 `<BookIcon>` (书页图标), `<CharacterSkin>` (角色皮肤，通常是游戏内建或自定义资源名) 等。

### 2. 定义被动能力 (Passive)

*   **目标文件**: `NormalInvitation/Data/PassiveList.xml`。
*   **作用**: 定义被动能力的核心数据，链接 C# 脚本（如果需要自定义逻辑）。
*   **操作**:
    *   为设定中的每个被动能力添加一个新的 `<Passive>` 节点。
    *   为其分配一个**唯一的纯数字 ID** (例如，从 9000001 开始，确保不与游戏或其他 Mod 冲突)。
    *   使用 `<Script>C#类名</Script>` 标签链接对应的 C# 实现类 (仅类名，无需命名空间或程序集)。如果被动只有属性加成（通常在核心书页中实现），则可能不需要 `<Script>`。
    *   配置 `<Rarity>`, `<Cost>`, `<Lock>`, `<CannotEquip>` 等属性。
    *   `<Name>` 和 `<Desc>` 在此文件中**仅作为占位符**或内部标识，实际显示文本需要在 `Localize` 文件中定义。

### 3. 定义战斗书页 (CardInfo)

*   **目标文件**: `NormalInvitation/Data/CardInfo.xml`。
*   **作用**: 定义角色使用的战斗书页。
*   **操作**:
    *   为每张卡牌添加一个新的 `<Card>` 节点。
    *   为其分配一个**唯一的纯数字 ID** (确保不冲突)。
    *   配置 `<Name>` (内部标识/占位符), `<TextId>` (链接本地化文件的 ID), `<Artwork>` (资源文件名) 等。
    *   **使用规范的字符串值 (基于参考 Mod "ColdSunOffice" 分析)**:
        *   **`<Rarity>`**: `Common`, `Uncommon`, `Rare`, `Unique` (可能还有 `Object`, `Exclusive` 等)
        *   **`<Spec Range="...">`**: `Near`, `Far`, `Instance`, `FarAreaEach`（群体攻击-交锋）, `FarArea`（群体攻击-清算）
        *   **`<Spec Affection="...">`**: `Team` (影响所有友方), `TeamNear` (影响附近友方)
        *   **`<Option>`**: `OnlyPage` (专属书页), `ExhaustOnUse` (使用后销毁), `Personal` (个人), `EgoPersonal` (EGO个人), `NoInventory` (不在书库显示) 等。
        *   **`<Keyword>`**: 可以是游戏内置状态 (如 `Burn`, `Bleed`, `Strength`) 或自定义字符串 (如 `onlypage_ColdSun_LY_Keyword`)，用于链接或标识。
        *   **`<Behaviour Type="...">`**: `Atk` (攻击), `Def` (防御), `Standby` (待机/特殊效果)。 (*注意: `Evasion` 不属于 Type, 见下*) 
        *   **`<Behaviour Detail="...">`**: `Hit` (打击), `Slash` (斩击), `Penetrate` (穿刺), `Guard` (防御), `Evasion` (回避)。
        *   **`<Behaviour Motion="...">`**: `H`, `J`, `Z`, `G`, `E` (通用动画), `F` (远程动画), `S1`, `S2`, `S3`, ... (特殊动画)。
    *   使用 `<Script>C#类名</Script>` 链接卡牌的主要效果脚本 (继承 `DiceCardSelfAbilityBase`)。
    *   在 `<BehaviourList>` 中定义骰子：
        *   配置 `<Behaviour>` 的 `Min`, `Dice`, `Type`, `Detail`, `Motion`。
        *   使用 `<Script>C#类名</Script>` 链接骰子的效果脚本 (继承 `DiceCardAbilityBase`)。
        *   使用 `<ActionScript>脚本名</ActionScript>` 链接特殊的动作脚本 (通常用于自定义动画或效果触发)。
        *   使用 `<EffectRes>资源名</EffectRes>` 指定骰子命中或防御时的特效资源。

### 4. 本地化文本

*   **目标文件**: `NormalInvitation/Localize/cn/...` (或其他语言目录)
*   **作用**: 提供 UI 中显示的文本。
*   **操作**: 为你在 `EquipPage`, `PassiveList`, `CardInfo` 等文件中定义的每个 ID，在对应的本地化文件中（如 `BookDesc/BookDesc.xml`, `PassiveDesc/PassiveDesc.xml`, `BattleCardDesc/BattleCardDesc.xml`）创建匹配的条目，并填写 `<Name>` 和 `<Desc>` (以及 `BehaviourDescList` 中的 `<BehaviourDesc>`)。

**极其重要**: 确保定义文件 (`Data/`) 中的数字 ID 与本地化文件 (`Localize/`) 中的 ID **完全一致**，游戏才能正确加载所有信息。

### 5. 编写 C# 代码 (按需)

*   **目标**: 你的 C# 项目 (例如 `MyDLL.csproj` 中的 `.cs` 文件)。
*   **作用**: 实现 XML 中定义的需要自定义逻辑的功能。
*   **操作**: 
    *   为 `<Script>` 标签引用的被动创建 C# 类，继承 `PassiveAbilityBase`。
    *   为 `<AbilityList>` 引用的卡牌效果创建 C# 类，继承 `DiceCardSelfAbilityBase`。
    *   为 `<BehaviourList>` 引用的骰子效果创建 C# 类，继承 `DiceCardAbilityBase`。
    *   为自定义 Buff (如"流") 创建 C# 类，继承 `BattleUnitBuf`。
    *   确保 C# 类名与 XML 中 `<Script>` 等标签填写的名称一致。

**重要**: 开始编辑前，强烈建议备份 `Data` 和 `Localize` 文件夹下的原始 XML/txt 文件。

### 6. 定义敌人卡组 (Deck)

*   **目标文件**: `NormalInvitation/Data/Deck_Enemy.xml`。
*   **作用**: 定义敌方单位具体使用的战斗书页卡组。
*   **操作**: 
    *   每个卡组对应一个 `<Deck ID="...">` 节点。这个 ID 通常与 `EnemyUnitInfo.xml` 中定义的敌人单位相关联 (通过 `<Deck>` 标签引用此 ID)。
    *   在 `<Deck>` 节点内部，使用 `<Card>卡牌ID</Card>` 标签列出卡组包含的每一张卡牌的 ID (这些 ID 必须是在 `CardInfo.xml` 中定义过的)。
    *   可以重复添加同一个卡牌 ID 来表示卡组中有多张该卡牌。

```xml
<!-- 示例: Data/Deck_Enemy.xml -->
<DeckXmlRoot ...>
  <Deck ID="MyDLL.EnemyDeck1">
    <Card>MyDLL.Anxier.ZhuoLueKongLiu</Card>
    <Card>MyDLL.Anxier.ZhuoLueKongLiu</Card>
    <Card>MyDLL.Anxier.HuiYiCeZhan</Card>
    <!-- ... 其他卡牌 -->
  </Deck>
</DeckXmlRoot>
```
