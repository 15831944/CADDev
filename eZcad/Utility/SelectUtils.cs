using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace eZcad.Utility
{
    public class SelectUtils
    {

        #region ---   从界面中选择对象

        /// <summary> 在界面中选择一个对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ed"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static T PickEntity<T>(Editor ed, string message = "选择一个对象") where T : Entity
        {
            var op = new PromptEntityOptions(message);
            op.SetRejectMessage($"请选择一个 {typeof(T).FullName} 对象");
            op.AddAllowedClass(typeof(T), exactMatch: false);
            var res = ed.GetEntity(op);
            if (res.Status == PromptStatus.OK)
            {
                return res.ObjectId.GetObject(OpenMode.ForRead) as T;
            }
            else
            {
                return null;
            }
        }

        /// <summary> 在界面中选择多个对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ed"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static T[] PickEntities<T>(Editor ed, string message = "选择多个对象") where T : Entity
        {
            var objectIds = PickEntities(ed, message);
            return objectIds.Select(id => id.GetObject(OpenMode.ForRead)).OfType<T>().ToArray();
        }

        /// <summary> 在界面中选择多个对象 </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ed"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ObjectId[] PickEntities(Editor ed, string message = "选择多个对象")
        {
            var entities = new ObjectId[0];
            var op = new PromptSelectionOptions()
            {

            };
            op.MessageForAdding = message;
            op.MessageForRemoval = message;


            //获取当前文档编辑器
            Editor acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;

            // 请求在图形区域选择对象
            var res = acDocEd.GetSelection(op);

            // 如果提示状态OK，表示对象已选
            if (res.Status == PromptStatus.OK)
            {
                entities = res.Value.GetObjectIds();
            }
            return entities;
        }

        #endregion

    }
}
