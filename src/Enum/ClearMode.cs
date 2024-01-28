using System;
using System.Collections.Generic;
using System.Text;

namespace Taurus.Plugin.DistributedTransaction
{
    /// <summary>
    /// 事务数据清除模式，仅对数据库有效，非数据库模式都是直接Delete模式。
    /// </summary>
    public enum ClearMode
    {
        /// <summary>
        /// 删除数据
        /// </summary>
        Delete = 0,
        /// <summary>
        /// 转移到历史表
        /// </summary>
        MoveToHistoryTable = 1
    }
}
