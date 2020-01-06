using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public interface IMusicStore
    {
        Music Get(string name);
    }
}