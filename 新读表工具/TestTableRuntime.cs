using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配置表运行时验证脚本(双模式)
///
/// 按 A 键: 模式A —— YOO 真实链路验证(TableBridge.Fetch + TableManager.LoadAllAsync)
///          验证 ProcedurePreload 实际使用的加载方式; 需在游戏正常启动流程后使用(YOO 已初始化)
/// 按 B 键: 模式B —— 磁盘/StreamingAssets 加载(TableManager.LoadAllFromDiskAsync)
///          编辑器: dataDir 填工程相对路径(如 Assets/Game/Download/DataTable)
///          真机/打包: dataDir 填 Application.streamingAssetsPath + "/子目录"
///          Android 的 jar: 路径自动走 UnityWebRequest, 无需额外处理
///
/// 说明:
///   - TestItem.serverOnly 为服务端专用列(s 端), 客户端生成类中不存在——
///     本文件没有引用它, 编译通过即为该项验证通过
/// </summary>
public class TestTableRuntime : MonoBehaviour
{
    [Header("模式A: YOO 异步链路(需游戏启动流程完成)")]
    public KeyCode yooTestKey = KeyCode.A;

    [Header("模式B: 磁盘/StreamingAssets 加载")]
    public KeyCode localTestKey = KeyCode.B;
    [Tooltip("编辑器: 工程相对路径; 真机: Application.streamingAssetsPath + \"/子目录\"")]
    public string dataDir = "Assets/Game/Download/DataTable";

    private int _pass;
    private int _fail;

    private void Update()
    {
        if (Input.GetKeyUp(yooTestKey))
        {
            RunYoo();
        }
        else if (Input.GetKeyUp(localTestKey))
        {
            RunDisk();
        }
    }

    // ======================================================================
    // 模式A: YOO 异步链路(与 ProcedurePreload 实际加载方式一致)
    // ======================================================================
    private async void RunYoo()
    {
        _pass = 0;
        _fail = 0;
        Debug.Log("[TestTable] ===== 模式A: YOO 链路(TableBridge.Fetch + LoadAllAsync) =====");
        try
        {
            await TableManager.LoadAllAsync(TableBridge.Fetch);
        }
        catch (Exception e)
        {
            Debug.LogError("[TestTable] YOO 加载异常(确认已走完游戏启动流程): " + e);
            return;
        }
        VerifyAll();
    }

    // ======================================================================
    // 模式B: 磁盘/StreamingAssets 加载(生成的正式磁盘入口, Android 自动 UnityWebRequest)
    // ======================================================================
    private async void RunDisk()
    {
        _pass = 0;
        _fail = 0;
        Debug.Log("[TestTable] ===== 模式B: 磁盘加载(LoadAllFromDiskAsync), 目录: " + dataDir + " =====");
        try
        {
            await TableManager.LoadAllFromDiskAsync(dataDir);
        }
        catch (Exception e)
        {
            Debug.LogError("[TestTable] 磁盘加载异常(检查 dataDir 路径): " + e);
            return;
        }
        VerifyAll();
    }

    // ======================================================================
    // 断言汇总
    // ======================================================================
    private void VerifyAll()
    {
        VerifyItem();
        VerifySkill();
        VerifyLevel();
        VerifyMonster();
        VerifyDialog();

        Debug.Log(string.Format("[TestTable] ===== 验证完成: 通过 {0}, 失败 {1} =====", _pass, _fail));
        if (_fail > 0)
        {
            Debug.LogError("[TestTable] 存在失败项, 请检查上方 [FAIL] 日志");
        }
    }

    private void Check(string name, bool ok, string detail = "")
    {
        if (ok)
        {
            _pass++;
            Debug.Log("[PASS] " + name + (detail == "" ? "" : "  -> " + detail));
        }
        else
        {
            _fail++;
            Debug.LogError("[FAIL] " + name + (detail == "" ? "" : "  -> " + detail));
        }
    }

    // ======================================================================
    // TestItem: 12种标量 + 容器 + 可空默认值 + range规则 + 端过滤
    // ======================================================================
    private void VerifyItem()
    {
        Check("Item 表行数=4(跳过注释/空行)",
              TableManager.Instance.TestItem.Dict.Count == 4,
              "实际=" + TableManager.Instance.TestItem.Dict.Count);

        Tab_TestItem item = TableManager.Instance.TestItem.GetDataByid(1001);
        Check("Item 1001 存在", item != null);
        if (item == null) return;

        Check("Item.name(string)", item.name == "血瓶", item.name);
        Check("Item.desc(string)", item.desc == "恢复生命值", item.desc);
        Check("Item.type(byte)", item.type == 1, item.type.ToString());
        Check("Item.price(int)", item.price == 50, item.price.ToString());
        Check("Item.discount(float)", item.discount == 0.8f, item.discount.ToString());
        Check("Item.weight(double)", item.weight == 1.25, item.weight.ToString());
        Check("Item.usable(bool=true)", item.usable, item.usable.ToString());
        Check("Item.stackSize(long)", item.stackSize == 9999999L, item.stackSize.ToString());
        Check("Item.equipLevel(short)", item.equipLevel == 10, item.equipLevel.ToString());
        Check("Item.pos(vector3)", item.pos == new Vector3(1, 2, 3), item.pos.ToString());
        Check("Item.offset(vector2)", item.offset == new Vector2(0.5f, 0f), item.offset.ToString());
        Check("Item.color(vector4)", item.color == new Vector4(1, 0, 0, 1), item.color.ToString());
        Check("Item.createTime(datetime)",
              item.createTime == new DateTime(2026, 1, 15, 8, 30, 0),
              item.createTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Check("Item.tags(list[string] 2元素)",
              item.tags != null && item.tags.Count == 2
              && item.tags[0] == "回复" && item.tags[1] == "药水");
        Check("Item.costs(list[int] 2元素)",
              item.costs != null && item.costs.Count == 2
              && item.costs[0] == 100 && item.costs[1] == 200);
        Check("Item.bonusAttr(map[int|float] 2条目)",
              item.bonusAttr != null && item.bonusAttr.Count == 2
              && item.bonusAttr.ContainsKey(1) && item.bonusAttr[1] == 1.5f
              && item.bonusAttr.ContainsKey(2) && item.bonusAttr[2] == 2.5f);
        Check("Item.quality(range规则通过=5)", item.quality == 5, item.quality.ToString());

        Tab_TestItem item2 = TableManager.Instance.TestItem.GetDataByid(1002);
        Check("Item 1002 desc(可空null->空串)", item2 != null && item2.desc == "",
              item2 == null ? "null" : "\"" + item2.desc + "\"");
        Check("Item 1002 usable(bool=false)", item2 != null && !item2.usable);
        Check("Item 1002 pos(负分量vector3)", item2 != null && item2.pos == new Vector3(-1, -2, 3));
        Check("Item 1002 color(vector4小数)",
              item2 != null && item2.color == new Vector4(0, 0, 1, 0.5f));

        Tab_TestItem item3 = TableManager.Instance.TestItem.GetDataByid(1003);
        Check("Item 1003 stackSize(int32上限值)",
              item3 != null && item3.stackSize == 2147483647L);
        Check("Item 1003 weight(double=0.01)",
              item3 != null && item3.weight == 0.01);

        Tab_TestItem item4 = TableManager.Instance.TestItem.GetDataByid(1004);
        Check("Item 1004 costs(3元素)",
              item4 != null && item4.costs != null && item4.costs.Count == 3);
        Check("Item 1004 bonusAttr(3条目)",
              item4 != null && item4.bonusAttr != null && item4.bonusAttr.Count == 3);
    }

    // ======================================================================
    // TestSkill: ref表间引用/自引用/list元素引用/len规则/跨字段gt/可空默认值
    // ======================================================================
    private void VerifySkill()
    {
        Check("Skill 表行数=3",
              TableManager.Instance.TestSkill.Dict.Count == 3);

        Tab_TestSkill s1 = TableManager.Instance.TestSkill.GetDataByid(1);
        Check("Skill 1 存在", s1 != null);
        if (s1 == null) return;
        Check("Skill name(len规则通过)", s1.name == "火球术", s1.name);
        Check("Skill itemId(ref表间引用)", s1.itemId == 1001, s1.itemId.ToString());
        Check("Skill preSkillId(可空null->0)", s1.preSkillId == 0, s1.preSkillId.ToString());
        Check("Skill costItems(ref列表元素)",
              s1.costItems != null && s1.costItems.Count == 2
              && s1.costItems[0] == 1001 && s1.costItems[1] == 1002);
        Check("Skill effectIds(2元素)",
              s1.effectIds != null && s1.effectIds.Count == 2
              && s1.effectIds[0] == 1 && s1.effectIds[1] == 2);
        Check("Skill 跨字段gt(cdEnd>cdStart)",
              s1.cdEnd > s1.cdStart, s1.cdStart + "->" + s1.cdEnd);
        Check("Skill power(float)", s1.power == 12.5f, s1.power.ToString());

        Tab_TestSkill s2 = TableManager.Instance.TestSkill.GetDataByid(2);
        Check("Skill 2 preSkillId(自引用=1)", s2 != null && s2.preSkillId == 1);
        Check("Skill 2 costItems(ref通过)",
              s2 != null && s2.costItems != null && s2.costItems.Count == 1
              && s2.costItems[0] == 1002);
        Check("Skill 2 effectIds(可空null->空列表)",
              s2 != null && s2.effectIds != null && s2.effectIds.Count == 0);

        Tab_TestSkill s3 = TableManager.Instance.TestSkill.GetDataByid(3);
        Check("Skill 3 costItems(3元素)",
              s3 != null && s3.costItems != null && s3.costItems.Count == 3);
        Check("Skill 3 power(float=20.25)", s3 != null && s3.power == 20.25f);
    }

    // ======================================================================
    // TestLevel: 复合KEY查询 + list[long] + map[int|string]
    // ======================================================================
    private void VerifyLevel()
    {
        Check("Level 表行数=3(纯List)",
              TableManager.Instance.TestLevel.List.Count == 3);

        Tab_TestLevel lv = TableManager.Instance.TestLevel.GetDataBylevelIdAndchapter(1, 2);
        Check("Level 复合KEY查询(1,2)存在", lv != null);
        if (lv == null) return;
        Check("Level name", lv.name == "迷雾森林", lv.name);
        Check("Level monsterIds(list[long])",
              lv.monsterIds != null && lv.monsterIds.Count == 1
              && lv.monsterIds[0] == 10003L);
        Check("Level rewards(map[int|string])",
              lv.rewards != null && lv.rewards.Count == 2
              && lv.rewards.ContainsKey(1003) && lv.rewards[1003] == "通关金币"
              && lv.rewards.ContainsKey(1004) && lv.rewards[1004] == "宝箱");
        Check("Level startPos(vector3)", lv.startPos == new Vector3(0, 5, 20));
        Check("Level timeLimit(float)", lv.timeLimit == 90.5f, lv.timeLimit.ToString());

        Tab_TestLevel lv2 = TableManager.Instance.TestLevel.GetDataBylevelIdAndchapter(2, 1);
        Check("Level 复合KEY查询(2,1)", lv2 != null && lv2.name == "熔岩洞窟",
              lv2 == null ? "null" : lv2.name);
        Check("Level (2,1) monsterIds(3元素)",
              lv2 != null && lv2.monsterIds != null && lv2.monsterIds.Count == 3);
        Check("Level (2,1) startPos(负分量)",
              lv2 != null && lv2.startPos == new Vector3(-5, 0, -5));

        Check("Level 复合KEY查询(不存在)返回null",
              TableManager.Instance.TestLevel.GetDataBylevelIdAndchapter(99, 99) == null);
    }

    // ======================================================================
    // TestMonster: 无KEY纯List访问 + map[int|vector3] + list[vector3]
    // ======================================================================
    private void VerifyMonster()
    {
        List<Tab_TestMonster> list = TableManager.Instance.TestMonster.List;
        Check("Monster 表行数=3(无KEY纯List)", list != null && list.Count == 3);
        if (list == null || list.Count < 3) return;

        Check("Monster[0] name/hp(long)",
              list[0].name == "哥布林" && list[0].hp == 5000L, list[0].name);
        Check("Monster[0] drops(map[int|vector3])",
              list[0].drops != null && list[0].drops.Count == 2
              && list[0].drops.ContainsKey(1) && list[0].drops[1] == new Vector3(1, 2, 3)
              && list[0].drops.ContainsKey(2) && list[0].drops[2] == new Vector3(4, 5, 6));
        Check("Monster[0] weaknessPoints(list[vector3] 2元素)",
              list[0].weaknessPoints != null && list[0].weaknessPoints.Count == 2
              && list[0].weaknessPoints[0] == new Vector3(1, 0, 1));
        Check("Monster[0] elite(bool=false)", !list[0].elite);

        Check("Monster[1] name/hp", list[1].name == "史莱姆" && list[1].hp == 3000L);
        Check("Monster[1] drops(1条目)",
              list[1].drops != null && list[1].drops.Count == 1
              && list[1].drops.ContainsKey(3) && list[1].drops[3] == new Vector3(7, 8, 9));

        Check("Monster[2] 巨龙elite(bool=true)",
              list[2].name == "巨龙" && list[2].elite && list[2].atk == 666);
        Check("Monster[2] drops(2条目)",
              list[2].drops != null && list[2].drops.Count == 2
              && list[2].drops.ContainsKey(4) && list[2].drops[4] == new Vector3(0, 100, 0));
        Check("Monster[2] weaknessPoints(3元素)",
              list[2].weaknessPoints != null && list[2].weaknessPoints.Count == 3);
    }

    // ======================================================================
    // TestDialog: string KEY查询 + datetime跨字段gt + map[string|int] + 可空
    // ======================================================================
    private void VerifyDialog()
    {
        Check("Dialog 表行数=2",
              TableManager.Instance.TestDialog.Dict.Count == 2);

        Tab_TestDialog d1 = TableManager.Instance.TestDialog.GetDataBydialogId("dlg_001");
        Check("Dialog stringKEY查询存在", d1 != null);
        if (d1 == null) return;
        Check("Dialog content(len规则通过)", d1.content == "你好，冒险者！", d1.content);
        Check("Dialog npcName", d1.npcName == "村长", d1.npcName);
        Check("Dialog startTime(datetime)",
              d1.startTime == new DateTime(2026, 1, 1), d1.startTime.ToString("yyyy-MM-dd"));
        Check("Dialog endTime(datetime+跨字段gt通过)",
              d1.endTime == new DateTime(2026, 12, 31, 23, 59, 59),
              d1.endTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Check("Dialog choices(list[string])",
              d1.choices != null && d1.choices.Count == 2
              && d1.choices[0] == "接受" && d1.choices[1] == "拒绝");
        Check("Dialog flags(map[string|int])",
              d1.flags != null && d1.flags.Count == 2
              && d1.flags.ContainsKey("flag_a") && d1.flags["flag_a"] == 1
              && d1.flags.ContainsKey("flag_b") && d1.flags["flag_b"] == 2);

        Tab_TestDialog d2 = TableManager.Instance.TestDialog.GetDataBydialogId("dlg_002");
        Check("Dialog 002 npcName(可空null->空串)", d2 != null && d2.npcName == "",
              d2 == null ? "null" : "\"" + d2.npcName + "\"");
        Check("Dialog 002 choices(可空null->空列表)",
              d2 != null && d2.choices != null && d2.choices.Count == 0);
        Check("Dialog 002 flags(flag_c=99)",
              d2 != null && d2.flags != null && d2.flags.Count == 2
              && d2.flags.ContainsKey("flag_c") && d2.flags["flag_c"] == 99);
    }
}
