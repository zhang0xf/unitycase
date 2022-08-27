using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NextSession
{
    private int _Seed;

    public NextSession()
    {
        _Seed = 1;
    }

    public NextSession(int _rhs)
    {
        _Seed = _rhs;
    }

    public int next()
    {
        return ((_Seed = _Seed * (int)214013L + (int)2531011L) >> 16) & 0x7fff;
    }
}
