using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum EventManageEnum
{

	
    #region 玉皆透澈声明的事件
    cardUsed,          // 卡牌使用事件
    ACardDragging,
    ACardOutDragging,

    drawPileOpen,
    drawPileClose,

    selectCardBegin,
    selectCardEnd,

    getSelectedCard,

    #endregion

    #region 卡牌管理器事件
    // 牌堆或手牌发生变化（已改为直接调用DUEL刷新UI）
    #endregion


}
