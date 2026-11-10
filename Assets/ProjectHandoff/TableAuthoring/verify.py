from pathlib import Path
from openpyxl import load_workbook
p=Path('Resources/FlowConfigV2')
def rows(name):
    w=load_workbook(p/(name+'.xlsx'));s=w.worksheets[0]
    assert all(c.comment for c in s[1])
    data=list(s.values); result=[dict(zip(data[0],r)) for r in data[1:]]
    assert len(result)==len({r[data[0][0]] for r in result})
    print(name,len(result),'rows;',len(data[0]),'annotated headers')
    return result
stages=rows('stage'); cps=rows('checkpoint'); stories=rows('story');rows('tutorial')
cm={c['checkpointId']:c for c in cps};sm={s['storyId']:s for s in stories};used=[]
for st in stages:
    ids=st['checkpointIds'].split(';'); selected=[cm[i] for i in ids];used+=ids
    days=[c['stageDay'] for c in selected]
    assert days==sorted(set(days))
    assert selected[-1]['checkpointType']=='Major'
    assert all(c['checkpointType']=='Minor' for c in selected[:-1])
    assert not st['nextStageId'] or st['nextStageId'] in {s['stageId'] for s in stages}
assert len(used)==len(set(used))==len(cps)
for c in cps:
    ids=c['storyIds'].split(';');assert len(ids)==len(set(ids))
    assert all(i in sm for i in ids)
    if c['checkpointType']=='Minor':assert sm[c['fallbackStoryId']]['conditionLua']=='return true'
    else:assert c['failureNode'] and not c['fallbackStoryId']
print('All stage/checkpoint/story references and ordering verified')
