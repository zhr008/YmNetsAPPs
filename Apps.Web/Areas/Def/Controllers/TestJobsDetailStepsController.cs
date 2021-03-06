﻿using System;
using System.Linq;
using Apps.Common;
using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Apps.DEF.IBLL;
using Apps.Models.DEF;
using Apps.Web.Core;
using System.Collections.Generic;
using Apps.Locale;

namespace Apps.Web.Areas.DEF.Controllers
{
    public class TestJobsDetailStepsController : BaseController
    {
        [Dependency]
        public IDEF_TestJobsDetailStepsBLL m_BLL { get; set; }

        [Dependency]
        public IDEF_TestJobsDetailItemBLL m_testItemBLL { get; set; }
        [Dependency]
        public IDEF_TestJobsBLL m_testJobsBLL { get; set; }
        [Dependency]
        public IDEF_DefectBLL m_defectBLL { get; set; }
        //错误集合
        ValidationErrors validationErrors = new ValidationErrors();
        /// <summary>
        /// 启动测试
        /// </summary>
        /// <returns></returns>
        public ActionResult RunTest()
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult CreateDefect(DEF_TestJobsDetailStepsModel model)
        {
            try
            {

                if (string.IsNullOrEmpty(model.Code))
                {
                    return Json(JsonHandler.CreateMessage(0, "测试用例不能为空,请选择一个"));
                }

                if (!ModelState.IsValid)
                {
                    return Json(JsonHandler.CreateMessage(0, "数据验证不通过"));
                }

                //新增
                model.ItemID = Guid.NewGuid().ToString();
                model.StepType = 1;
                model.Tester = GetUserId();
                model.TestDt = DateTime.Now;

                m_BLL.CreateDefect(ref validationErrors, model, GetUserId());
                //写日志
                if (validationErrors.Count > 0)
                {
                    //错误写入日志
                    LogHandler.WriteServiceLog(GetUserId(), Resource.InsertFail + "，新增测试步骤ID:" + model.ItemID,  "失败", "新增", "测试步骤");
                    return Json(JsonHandler.CreateMessage(0, validationErrors.Error));
                }
                //成功写入日志
                LogHandler.WriteServiceLog(GetUserId(), Resource.InsertSucceed + "，新增测试步骤ID:" + model.ItemID,  "成功", "新增", "测试步骤");
                return Json(JsonHandler.CreateMessage(1, Resource.InsertSucceed));
            }
            catch
            {
                return Json(JsonHandler.CreateMessage(1, Resource.InsertFail));
            }
        }
        public ActionResult CreateDefect(string vercode, string code)
        {
            if (!ModelState.IsValid)
            {
                return View("数据验证不通过", true);
            }
            if (string.IsNullOrEmpty(vercode))
            {
                return View("没有选择测试任务", true);
            }
            if (string.IsNullOrEmpty(code))
            {
                code = "";
            }
            //
            string codeName = "";
            var testItem = m_testItemBLL.GetModelById(vercode, code);
            if (testItem != null)
            {
                codeName = testItem.Name;
            }
            else
            {
                codeName = "未选择测试用例";
            }
            DEF_TestJobsDetailStepsModel stepsModel = new DEF_TestJobsDetailStepsModel()
            {
                VerCode = vercode,
                Code = code,
                ItemID = "0",
                Result = false,
                Sort = 100,
                StepType = 1

            };
            return View(stepsModel);
        }

        //详细主页
        [SupportFilter]
        public ActionResult Index()
        {
            ViewBag.perm = GetPermission();
            var testJobs = m_testJobsBLL.GetDefaultTestJobs(ref validationErrors);
            if (testJobs != null)
            {
                ViewBag.vercode = testJobs.VerCode;
                ViewBag.name = testJobs.Name;
            }
            return View();
        }

        /// <summary>
        /// 执行开发
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        //[SupportFilter]
        [ValidateInput(false)]
        public ActionResult RunDev(DEF_TestJobsDetailStepsModel model)
        {
            try
            {
                model.Developer = GetUserId();
                model.FinDt = DateTime.Now;
                if (m_BLL.RunDev(ref validationErrors, model))
                {
                    //生成缺陷报告
                    m_testItemBLL.DevUpdateState(ref validationErrors, model.VerCode, model.Code);

                }
                //写日志
                if (validationErrors.Count > 0)
                {
                    //错误写入日志
                    LogHandler.WriteServiceLog(GetUserId(), Resource.UpdateFail + "，新增测试步骤ID:" + model.ItemID, "失败", "新增", "测试步骤");
                    return Json(JsonHandler.CreateMessage(0, validationErrors.Error));
                }
                //成功写入日志
                LogHandler.WriteServiceLog(GetUserId(), Resource.UpdateFail + "，新增测试步骤ID:" + model.ItemID, "成功", "新增", "测试步骤");
                return Json(JsonHandler.CreateMessage(1, Resource.UpdateSucceed));
            }
            catch
            {
                return Json(JsonHandler.CreateMessage(0, Resource.UpdateFail));
            }

        }
        //JQGrid获取
        [HttpPost]
        public JsonResult GetList(GridPager pager, string queryStr, string vercode)
        {
            List<DEF_TestJobsDetailStepsModel> list = m_BLL.GetList(ref pager, queryStr, vercode); 
            var json = new
            {
                total = pager.totalRows,
                rows = (from r in list
                        select new DEF_TestJobsDetailStepsModel()
                        {
                            Id = r.ItemID + "_" + r.VerCode + "_" + r.Code,//ID
                            ItemID = r.ItemID,
                            VerCode = r.VerCode,
                            Code = r.Code,
                            Title = r.Title,
                            TestContent = r.TestContent,
                            Result = r.Result,
                            Sort = r.Sort,
                            ResultContent = r.ResultContent,
                            ExSort = r.ExSort,
                            StepType = r.StepType,//0?"测试步骤":"缺陷记录",
                            Tester = r.Tester,
                            TestDt = r.TestDt,
                            Developer = r.Developer,
                            PlanStartDt = r.PlanStartDt,
                            PlanEndDt = r.PlanEndDt,
                            FinDt = r.FinDt,
                            TestRequestFlag = r.TestRequestFlag,
                            DevFinFlag = r.DevFinFlag
                        }).ToArray()
            };
            return Json(json);
        }
        public JsonResult GetOne(string itemId, string vercode, string code)
        {

            try
            {
                var model = m_BLL.GetModelById(itemId, vercode, code);

                return Json(model);

            }
            catch (Exception ex)
            {
                LogHandler.WriteServiceLog(GetUserId(), "读取出错,查询参数:" + itemId + " 错误：" + ex.Message, "失败", "读取", "测试步骤");
                return null;
            }

        }
        public JsonResult GetListNoAction(GridPager pager, string vercode, string code)
        {
            List<DEF_TestJobsDetailStepsModel> list = m_BLL.GetListByCode(ref pager, vercode, code, "");
            var json = new
            {
                total = pager.totalRows,
                rows = (from r in list
                        select new DEF_TestJobsDetailStepsModel()
                        {
                            Id = r.ItemID + "_" + r.VerCode + "_" + r.Code,//ID
                            ItemID = r.ItemID,
                            VerCode = r.VerCode,
                            Code = r.Code,
                            Title = r.Title,
                            TestContent = r.TestContent,
                            Result = r.Result,
                            Sort = r.Sort,
                            ResultContent = r.ResultContent,
                            ExSort = r.ExSort,
                            StepType = r.StepType,//0?"测试步骤":"缺陷记录",
                            Tester = r.Tester,
                            TestDt = r.TestDt,
                            Developer = r.Developer,
                            PlanStartDt = r.PlanStartDt,
                            PlanEndDt = r.PlanEndDt,
                            FinDt = r.FinDt,
                            TestRequestFlag = r.TestRequestFlag,
                            DevFinFlag = r.DevFinFlag
                        }).ToArray()
            };
            return Json(json);

        }
        public JsonResult GetListByCode(GridPager pager, string vercode, string code)
        {
            List<DEF_TestJobsDetailStepsModel> list = m_BLL.GetListByCode(ref pager, vercode, code,"");
            var json = new
            {
                total = pager.totalRows,
                rows = (from r in list
                        select new DEF_TestJobsDetailStepsModel()
                        {
                            Id = r.ItemID + "_" + r.VerCode + "_" + r.Code,
                            ItemID = r.ItemID,
                            VerCode = r.VerCode,
                            Code = r.Code,
                            Title = r.Title,
                            TestContent = r.TestContent,
                            Result = r.Result,
                            Sort = r.Sort,
                            ResultContent = r.ResultContent,
                            ExSort = r.ExSort,
                            StepType = r.StepType,
                            Tester = r.Tester,
                            TestDt = r.TestDt,
                            Developer = r.Developer,
                            PlanStartDt = r.PlanStartDt,
                            PlanEndDt = r.PlanEndDt,
                            FinDt = r.FinDt,
                            TestRequestFlag = r.TestRequestFlag,
                            DevFinFlag = r.DevFinFlag
                        }).ToArray()
            };
            return Json(json);
        }

        //新增
        public ActionResult Create(string vercode, string code)
        {
            if (!ModelState.IsValid)
            {
                return View("数据验证不通过", true);
            }
            if (string.IsNullOrEmpty(vercode))
            {
                return View("没有选择测试任务", true);
            }
            if (string.IsNullOrEmpty(code))
            {
                return View("没有选择测试项目", true);
            }
            DEF_TestJobsDetailItemModel model = m_testItemBLL.GetModelById(vercode, code);
            if (model == null)
            {
                return View("测试项目不存在");
            }

            DEF_TestJobsDetailStepsModel stepsModel = new DEF_TestJobsDetailStepsModel()
            {
                VerCode = vercode,
                Code = code,
                ItemID = "0",
                Sort = 0,
                StepType = 0
            };
            return View(stepsModel);
        }

        //创建提交
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult Create(DEF_TestJobsDetailStepsModel model)
        {
            try
            {
            
                //新增
                model.ItemID = Guid.NewGuid().ToString();
                model.StepType = 0;
                m_BLL.Create(ref validationErrors, model);
                //写日志
                if (validationErrors.Count > 0)
                {
                    //错误写入日志
                    LogHandler.WriteServiceLog(GetUserId(), Resource.InsertFail + "，新增测试步骤ID:" + model.ItemID, "失败", "新增", "测试步骤");
                    return Json(JsonHandler.CreateMessage(0, validationErrors.Error));
                }
                //成功写入日志
                LogHandler.WriteServiceLog(GetUserId(), Resource.InsertSucceed + "，新增测试步骤ID:" + model.ItemID, "成功", "新增", "测试步骤");
                return Json(JsonHandler.CreateMessage(1, Resource.InsertSucceed));
            }
            catch
            {
                return Json(JsonHandler.CreateMessage(1, Resource.InsertFail));
            }
        }
        //修改
        public ActionResult Edit(string itemid, string vercode, string code)
        {
            if (!ModelState.IsValid)
            {
                return View("数据验证不通过", true);
            }
            DEF_TestJobsDetailStepsModel model = m_BLL.GetModelById(itemid, vercode, code);

            return View(model);

        }
        //修改
        [HttpPost]
        //[SupportFilter]
        [ValidateInput(false)]
        public ActionResult Edit(DEF_TestJobsDetailStepsModel model)
        {
            try
            {
                m_BLL.Edit(ref validationErrors, model);
                //写日志
                if (validationErrors.Count > 0)
                {
                    //错误写入日志
                    LogHandler.WriteServiceLog(GetUserId(), Resource.UpdateFail + "，编辑测试步骤ID:" + model.ItemID, "失败", "编辑", "测试步骤");
                    return Json(JsonHandler.CreateMessage(0, validationErrors.Error));
                }
                //成功写入日志
                LogHandler.WriteServiceLog(GetUserId(), Resource.UpdateSucceed + "，编辑测试步骤ID:" + model.ItemID, "成功", "编辑", "测试步骤");
                return Json(JsonHandler.CreateMessage(1, Resource.UpdateSucceed));
            }
            catch
            {
                return Json(JsonHandler.CreateMessage(0, Resource.UpdateFail));
            }

        }
        //测试
        [HttpPost]
        //[SupportFilter]
        [ValidateInput(false)]
        public ActionResult RunTest(DEF_TestJobsDetailStepsModel model)
        {
            try
            {
                model.Tester = GetUserId();
                model.TestDt = DateTime.Now;
                if (m_BLL.RunTest(ref validationErrors, model))
                {
                    //生成缺陷报告
                    m_defectBLL.CreateDefectReport(ref validationErrors, model.VerCode, GetUserId());
                }
                //写日志
                if (validationErrors.Count > 0)
                {
                    //错误写入日志
                    LogHandler.WriteServiceLog(GetUserId(), Resource.UpdateFail + "，编辑测试步骤ID:" + model.ItemID, "失败", "编辑", "测试步骤");
                    return Json(JsonHandler.CreateMessage(0, validationErrors.Error));
                }
                //成功写入日志
                LogHandler.WriteServiceLog(GetUserId(), Resource.UpdateSucceed + "，编辑测试步骤ID:" + model.ItemID, "成功", "编辑", "测试步骤");
                return Json(JsonHandler.CreateMessage(1, Resource.UpdateSucceed));
            }
            catch
            {
                return Json(JsonHandler.CreateMessage(0, Resource.UpdateFail));
            }

        }

        //详细
        public ActionResult Details(string itemid, string vercode, string code)
        {
            if (!ModelState.IsValid)
            {
                return View("数据验证不通过", true);
            }
            DEF_TestJobsDetailStepsModel model = m_BLL.GetModelById(itemid, vercode, code);

            return View(model);

        }

        // 删除 
        [HttpPost]
        public JsonResult Delete(string itemid, string vercode, string code)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(JsonHandler.CreateMessage(0, "数据验证不通过"));
                }
                if (string.IsNullOrEmpty(itemid))
                {
                    return Json(JsonHandler.CreateMessage(0, "请选择删除记录"));
                }
                if (string.IsNullOrEmpty(code))
                {
                    return Json(JsonHandler.CreateMessage(0, "请选择删除记录"));
                }
                if (string.IsNullOrEmpty(vercode))
                {
                    return Json(JsonHandler.CreateMessage(0, "请选择版本"));
                }

                m_BLL.Delete(ref validationErrors, itemid, vercode, code);

                //写日志
                if (validationErrors.Count > 0)
                {
                    //错误写入日志
                    LogHandler.WriteServiceLog(GetUserId(), Resource.DeleteFail + "，删除测试步骤ID:" + itemid, "失败", "删除", "测试步骤");
                    return Json(JsonHandler.CreateMessage(0, validationErrors.Error));
                }
                //成功写入日志
                LogHandler.WriteServiceLog(GetUserId(), Resource.DeleteSucceed + "，删除测试步骤ID:" + itemid, "成功", "删除", "测试步骤");
                return Json(JsonHandler.CreateMessage(1, Resource.DeleteSucceed));
            }
            catch
            {
                return Json(JsonHandler.CreateMessage(1, Resource.DeleteFail));
            }
        }
    }
}
