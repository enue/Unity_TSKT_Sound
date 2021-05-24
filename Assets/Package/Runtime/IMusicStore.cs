using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

namespace TSKT
{
    public interface IMusicStore
    {
        Music? Get(string name);
    }
}