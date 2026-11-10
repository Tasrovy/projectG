import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from '@oai/artifact-tool';
const out='D:/Documents/GitHub/projectG/Assets/Resources/FlowConfigV2';
await fs.mkdir(out,{recursive:true});
const specs=[
['stage',[['stageId','string','阶段唯一ID，非空且不可重复'],['stageOrder','int','阶段顺序，正整数'],['minorCheckDays','string','阶段内操作日检查点，英文分号分隔，例如3;5。教程和跳过的周末不计数'],['majorCheckDay','int','阶段内大高潮日，必须晚于小高潮；同日时大高潮替代小高潮'],['minorPoolId','string','引用story表的小高潮poolId'],['majorPoolId','string','引用story表的大高潮poolId'],['fallbackStoryId','string','小高潮无合格候选时播放的Fallback故事ID，可重复播放'],['failureNode','string','大高潮无合格候选时播放的Yarn入口；结束后失败'],['nextStageId','string','成功后下一阶段ID，最终阶段留空'],['successNode','string','最终阶段成功后播放的结局入口；非最终阶段可空']],
[['stage_01',1,'3;5',8,'minor_01','major_01','ordinary_01','TODO_fail_01','stage_02',''],['stage_02',2,'3;5',8,'minor_02','major_02','ordinary_02','TODO_fail_02','','TODO_success']]],
['story',[['storyId','string','剧情唯一ID，同时是图鉴记录键。发布后避免改名'],['displayName','string','策划阅读名称，不参与判断'],['poolId','string','候选池ID，阶段表引用；Fallback通过storyId直接引用'],['storyType','enum','Minor小高潮 / Major大高潮 / Fallback平庸剧情'],['entryNode','string','整段剧情的Yarn入口。TODO_开头均为待替换示例'],['conditionLua','string','Lua代码，必须return布尔值。空默认true；配置错误不能当作游戏失败'],['enabled','bool','true启用，false禁用'],['notes','string','策划备注，不参与执行']],
[['coffee_01','咖啡馆邀约','minor_01','Minor','TODO_coffee_01','return stats.nature1 >= 30',true,'阈值为示例'],['walk_01','放学同行','minor_01','Minor','TODO_walk_01','return stats.nature2 >= 20 and stats.nature3 >= 10',true,'满足多个时优先图鉴未看'],['ordinary_01','普通的一天','fallback_01','Fallback','TODO_ordinary_01','return true',true,'无合格小高潮时播放，允许重复'],['major_01_a','第一次关系突破','major_01','Major','TODO_major_01_a','return stats.nature1 >= 60',true,'看过仍可重复进入'],['major_01_b','互相依靠','major_01','Major','TODO_major_01_b','return stats.nature2 >= 50 and stats.nature3 >= 30',true,'无合格大高潮则失败'],['library_02','图书馆约会','minor_02','Minor','TODO_library_02','return stats.nature1 >= 80',true,'第二阶段示例'],['promise_02','兑现约定','minor_02','Minor','TODO_promise_02','return stats.nature3 >= 50 and seenThisRun("coffee_01")',true,'本轮前置，不是跨周目图鉴'],['ordinary_02','平静的校园生活','fallback_02','Fallback','TODO_ordinary_02','return true',true,'可重复兜底'],['major_02_a','共同的未来','major_02','Major','TODO_major_02_a','return stats.charm >= 180',true,'最终阶段示例'],['major_02_b','坚定的选择','major_02','Major','TODO_major_02_b','return stats.nature3 >= 100 and not flag("relationship_broken")',true,'标记函数待接入']]],
['tutorial',[['stepId','string','教程步骤唯一ID'],['order','int','执行顺序，正整数且不重复'],['entryNode','string','步骤开始提示Yarn入口，可空'],['uiMode','enum','Dialogue / Craft / Shop；逻辑模式，需映射现有界面'],['allowedAction','enum','None / PlayCard / Combine / Dismantle / Attach / Buy / Sell / Free；需接真实操作事件'],['completionEvent','enum','DialogueCompleted / CardPlayed / CardCombined / CardDismantled / CardAttached / ItemBought / CardSold'],['targetId','string','匹配操作对象的卡牌/商品ID，空表示任意。合成按结果卡ID匹配'],['requiredCount','int','本步骤成功事件次数，正整数；开始步骤时清零'],['notes','string','教学资源和约束说明，非可执行配置']],
[['tutorial_intro',1,'TODO_tutorial_intro','Dialogue','None','DialogueCompleted','',1,'教程不计正式操作日；剧情完成指整段链结束'],['tutorial_play',2,'TODO_tutorial_play','Craft','PlayCard','CardPlayed','',1,'用固定教学手牌；初始化资源逻辑待实现'],['tutorial_combine',3,'TODO_tutorial_combine','Craft','Combine','CardCombined','',1,'准备可合成材料；成功结算后发事件'],['tutorial_dismantle',4,'TODO_tutorial_dismantle','Craft','Dismantle','CardDismantled','',1,'准备可拆卡'],['tutorial_attach',5,'TODO_tutorial_attach','Craft','Attach','CardAttached','',1,'准备附加材料与目标卡'],['tutorial_buy',6,'TODO_tutorial_buy','Shop','Buy','ItemBought','',1,'保证教学金钱足够'],['tutorial_sell',7,'TODO_tutorial_sell','Shop','Sell','CardSold','',1,'保证有可出售手牌'],['tutorial_end',8,'TODO_tutorial_end','Dialogue','None','DialogueCompleted','',1,'完成后重置正式开局资源并进入stage_01']]]
];
const col=n=>String.fromCharCode(65+n);
// 小高潮按检查点独立配置；旧阶段列保留以便迁移，但不再作为运行时来源。
const stage=specs.find(s=>s[0]==='stage');
for(const i of [2,4,6])stage[1][i][2]='已弃用，保留旧列；请在checkpoint.xlsx逐检查点配置，不读取本列';
const story=specs.find(s=>s[0]==='story');
story[1][2][2]='剧情池ID；小高潮由checkpoint.poolId引用，大高潮由stage.majorPoolId引用';
story[2]=story[2].filter(r=>r[3]!=='Minor');
for(let st=1;st<=2;st++)for(let dayIndex=0;dayIndex<2;dayIndex++)for(let n=1;n<=3;n++){
 const day=dayIndex===0?3:5,id=`s${st}_minor_${dayIndex*3+n}`;
 story[2].push([id,`阶段${st}小高潮${dayIndex*3+n}`,`s${st}_day${day}`,'Minor',`TODO_${id}`,`return stats.nature${n} >= ${20+dayIndex*20+(st-1)*40}`,true,`仅阶段${st}第${day}操作日候选，示例阈值`]);
}
specs.push(['checkpoint',[
 ['checkpointId','string','检查点唯一ID，保存本轮结算状态使用'],
 ['stageId','string','所属阶段，引用stage.stageId'],
 ['stageDay','int','阶段内第几个正式操作日结束时检查；教程和跳过周末不计'],
 ['poolId','string','该检查点专属Minor剧情池，只在此池筛选'],
 ['fallbackStoryId','string','本检查点无合格候选时播放的Fallback storyId，可重复'],
 ['enabled','bool','true启用，false禁用']
],[[ 's1_cp_03','stage_01',3,'s1_day3','ordinary_01',true],['s1_cp_05','stage_01',5,'s1_day5','ordinary_01',true],['s2_cp_03','stage_02',3,'s2_day3','ordinary_02',true],['s2_cp_05','stage_02',5,'s2_day5','ordinary_02',true]]]);
// V3：关联仅由父表持有，取消池与反向stageId，数量从列表推导。
stage[1]=[['stageId','string','阶段唯一ID'],['checkpointIds','string','本阶段全部检查点ID，英文分号分隔；按日期递增，末项必须为Major'],['nextStageId','string','成功后下一阶段ID；最终阶段留空'],['successNode','string','最终阶段成功后的Yarn结局入口，非最终阶段可空']];
stage[2]=[['stage_01','s1_cp_03;s1_cp_05;s1_cp_08','stage_02',''],['stage_02','s2_cp_03;s2_cp_05;s2_cp_08','','TODO_success']];
story[1]=story[1].filter(f=>!['poolId','storyType'].includes(f[0]));
story[2]=story[2].map(r=>[r[0],r[1],r[4],r[5],r[6],r[7]]);
const cp=specs.find(s=>s[0]==='checkpoint');
cp[1]=[['checkpointId','string','唯一检查点ID，由stage.checkpointIds引用'],['stageDay','int','阶段内第几个操作日结束时判定，从1开始，教程不计'],['checkpointType','enum','Minor小高潮 / Major大高潮；阶段末项必须Major'],['storyIds','string','本检查点全部候选storyId，英文分号分隔，数量自动取列表长度'],['fallbackStoryId','string','Minor无合格剧情时的兜底storyId；Major留空'],['failureNode','string','Major无合格剧情时的Yarn失败入口；Minor留空']];
cp[2]=[];
for(let st=1;st<=2;st++){
 for(const [day,start] of [[3,1],[5,4]])cp[2].push([`s${st}_cp_0${day}`,day,'Minor',[start,start+1,start+2].map(n=>`s${st}_minor_${n}`).join(';'),`ordinary_0${st}`,'']);
 cp[2].push([`s${st}_cp_08`,8,'Major',`major_0${st}_a;major_0${st}_b`,'',`TODO_fail_0${st}`]);
}
for(const [name,fields,rows] of specs){
 const wb=Workbook.create(), sh=wb.worksheets.add(name), doc=wb.worksheets.add('字段说明');
 sh.getRangeByIndexes(0,0,rows.length+1,fields.length).values=[fields.map(f=>f[0]),...rows];
 doc.getRangeByIndexes(0,0,fields.length+1,3).values=[['字段','类型','作用与填写规则'],...fields];
 const authorId=wb.comments.setSelf({displayName:'User'}).id;
 for(let i=0;i<fields.length;i++)wb.notes.add({id:`${name}:${col(i)}1`,target:{cell:{sheetName:name,sheetId:sh.sheetId,address:`${col(i)}1`}},authorId,createdAt:'',body:{plainText:`${fields[i][1]}：${fields[i][2]}`}});
 for(const s of [sh,doc]){s.showGridLines=false;const r=s.getUsedRange();r.format.font={name:'Arial',size:11};r.format.rowHeight=40;r.format.columnWidth=23;r.format.wrapText=true;r.format.verticalAlignment='center';s.getRangeByIndexes(0,0,1,s===sh?fields.length:3).format={fill:'#40365C',font:{bold:true,color:'#FFFFFF'},rowHeight:34,wrapText:true};s.freezePanes.freezeRows(1);}
 doc.getRange('C:C').format.columnWidth=85;doc.getRange('A:A').format.columnWidth=26;
 if(name==='story'){sh.getRange('D:D').format.columnWidth=70;sh.getRange('F:F').format.columnWidth=42;}
 if(name==='stage')sh.getRange('B:B').format.columnWidth=48;
 if(name==='checkpoint'){sh.getRange('D:D').format.columnWidth=65;sh.getRange('C2:C1000').dataValidation={rule:{type:'list',values:['Minor','Major']}};}
 if(name==='tutorial')sh.getRange('I:I').format.columnWidth=58;
 wb.recalculate();
 console.log((await wb.inspect({kind:'table',range:`${name}!A1:D3`,tableMaxRows:3,tableMaxCols:4,maxChars:1200})).ndjson);
 for(const s of [sh,doc]){const png=await wb.render({sheetName:s.name,range:s===sh?'A1:D6':'A1:C6',scale:1,format:'png'});await fs.writeFile(`D:/Documents/GitHub/projectG/Assets/ProjectHandoff/TableAuthoring/${name}-${s===sh?'data':'fields'}.png`,new Uint8Array(await png.arrayBuffer()));}
 await(await SpreadsheetFile.exportXlsx(wb)).save(`${out}/${name}.xlsx`);
}
