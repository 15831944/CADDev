﻿using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace AutoCADDev
{
    /// <summary> 对文档进行配置，以启动文档的改写模式 </summary>
    public class DocumentModifier : IDisposable
    {
        public readonly Transaction acTransaction;
        /// <summary> 当前活动的AutoCAD文档 </summary>
        public readonly Document acActiveDocument;
        /// <summary> 当前活动的AutoCAD文档中的数据库 </summary>
        public readonly Database acDataBase;

        private readonly DocumentLock acLock;

        /// <summary> 对文档进行配置，以启动文档的改写模式 </summary>
        public DocumentModifier()
        {
            // 获得当前文档和数据库   Get the current document and database
            acActiveDocument = Application.DocumentManager.MdiActiveDocument;
            acDataBase = acActiveDocument.Database;
            //
            acLock = acActiveDocument.LockDocument();
            acTransaction = acDataBase.TransactionManager.StartTransaction();
        }

        #region IDisposable Support

        private bool valuesDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!valuesDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    acTransaction.Dispose();
                    acLock.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                valuesDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~DocumentModifier()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}