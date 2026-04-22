using System;
using UnityEngine;

public sealed class CardNatureState
{
    private readonly Card _card;

    public CardNatureState(Card card)
    {
        _card = card;
    }

    public void Add(int id, int num)
    {
        switch (id)
        {
            case 1: AddNature1(num); break;
            case 2: AddNature2(num); break;
            case 3: AddNature3(num); break;
            default: Debug.LogError($"unknown id :{id}"); break;
        }
    }

    public void AddTo(int id, int num)
    {
        switch (id)
        {
            case 1: AddNature1To(num); break;
            case 2: AddNature2To(num); break;
            case 3: AddNature3To(num); break;
            default: Debug.LogError($"unknown id :{id}"); break;
        }
    }

    public int GetNatureById(int id)
    {
        switch (id)
        {
            case 1: return _card.nature1;
            case 2: return _card.nature2;
            case 3: return _card.nature3;
            default: return 0;
        }
    }

    private void AddNature1(int num)
    {
        if (_card.nature1 == 0) return;
        int target = _card.nature1 + num;
        if (_card.nature1 < 0) _card.nature1 = target > 0 ? 0 : target;
        else if (_card.nature1 > 0) _card.nature1 = target < 0 ? 0 : target;
    }

    private void AddNature2(int num)
    {
        if (_card.nature2 == 0) return;
        int target = _card.nature2 + num;
        if (_card.nature2 < 0) _card.nature2 = target > 0 ? 0 : target;
        else if (_card.nature2 > 0) _card.nature2 = target < 0 ? 0 : target;
    }

    private void AddNature3(int num)
    {
        if (_card.nature3 == 0) return;
        int target = _card.nature3 + num;
        if (_card.nature3 < 0) _card.nature3 = target > 0 ? 0 : target;
        else if (_card.nature3 > 0) _card.nature3 = target < 0 ? 0 : target;
    }

    private void AddNature1To(int num)
    {
        if (_card.nature1 == 0) return;
        int safeNum = num;
        if (_card.nature1 < 0 && num > 0) safeNum = 0;
        else if (_card.nature1 > 0 && num < 0) safeNum = 0;
        if (_card.nature1 < safeNum) _card.nature1 = safeNum;
    }

    private void AddNature2To(int num)
    {
        if (_card.nature2 == 0) return;
        int safeNum = num;
        if (_card.nature2 < 0 && num > 0) safeNum = 0;
        else if (_card.nature2 > 0 && num < 0) safeNum = 0;
        if (_card.nature2 < safeNum) _card.nature2 = safeNum;
    }

    private void AddNature3To(int num)
    {
        if (_card.nature3 == 0) return;
        int safeNum = num;
        if (_card.nature3 < 0 && num > 0) safeNum = 0;
        else if (_card.nature3 > 0 && num < 0) safeNum = 0;
        if (_card.nature3 < safeNum) _card.nature3 = safeNum;
    }
}
