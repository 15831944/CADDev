﻿using DllActivator;

namespace eZcad
{
    /// <summary> 用于 AddinManager 中调试 dll 时将引用的程序集加载到进程中 </summary>
    public class DllActivator_RESD : IDllActivator_std
    {
        /// <summary>
        /// 激活本DLL所引用的那些DLLs
        /// </summary>
        public void ActivateReferences()
        {
            IDllActivator_std dat1 = new DllActivator_std();
            dat1.ActivateReferences();
            //
            dat1 = new DllActivator_eZx_API();
            dat1.ActivateReferences();

        }
    }
}