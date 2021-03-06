using System;
using System.Collections.Specialized;
using System.Web.UI.WebControls;
using BaiRong.Core;
using SiteServer.BackgroundPages.Ajax;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.Create;
using SiteServer.CMS.Model.Enumerations;

namespace SiteServer.BackgroundPages.Cms
{
    public class PageProgressBar : BasePageCms
    {
        public Literal LtlTitle;
        public Literal LtlRegisterScripts;

        protected override bool IsSinglePage => true;

        public static readonly string CookieNodeIdCollection = "SiteServer.BackgroundPages.Cms.PageProgressBar.NodeIdCollection ";

        public static readonly string CookieContentIdentityCollection = "SiteServer.BackgroundPages.Cms.PageProgressBar.ContentIdentityCollection";

        public static readonly string CookieTemplateIdCollection = "SiteServer.BackgroundPages.Cms.PageProgressBar.TemplateIdCollection";

        public static string GetCreatePublishmentSystemUrl(int publishmentSystemId, bool isImportContents, bool isImportTableStyles, string siteTemplateDir, string onlineTemplateName, bool isUseTables, string userKeyPrefix)
        {
            return PageUtils.GetCmsUrl(nameof(PageProgressBar), new NameValueCollection
            {
                {"createPublishmentSystem", true.ToString()},
                {"publishmentSystemId", publishmentSystemId.ToString()},
                {"isImportContents", isImportContents.ToString()},
                {"isImportTableStyles", isImportTableStyles.ToString()},
                {"siteTemplateDir", siteTemplateDir},
                {"onlineTemplateName", onlineTemplateName},
                {"isUseTables", isUseTables.ToString()},
                {"userKeyPrefix", userKeyPrefix}
            });
        }

        public static string GetBackupUrl(int publishmentSystemId, string backupType, string userKeyPrefix)
        {
            return PageUtils.GetCmsUrl(nameof(PageProgressBar), new NameValueCollection
            {
                {"publishmentSystemId", publishmentSystemId.ToString()},
                {"backup", true.ToString()},
                {"backupType", backupType},
                {"userKeyPrefix", userKeyPrefix}
            });
        }

        public static string GetRecoveryUrl(int publishmentSystemId, string isDeleteChannels, string isDeleteTemplates, string isDeleteFiles, bool isZip, string path, string isOverride, string isUseTable, string userKeyPrefix)
        {
            return PageUtils.GetCmsUrl(nameof(PageProgressBar), new NameValueCollection
            {
                {"publishmentSystemId", publishmentSystemId.ToString()},
                {"recovery", true.ToString()},
                {"isDeleteChannels", isDeleteChannels},
                {"isDeleteTemplates", isDeleteTemplates},
                {"isDeleteFiles", isDeleteFiles},
                {"isZip", isZip.ToString()},
                {"path", path},
                {"isOverride", isOverride},
                {"isUseTable", isUseTable},
                {"userKeyPrefix", userKeyPrefix}
            });
        }

        public static string GetDeleteAllPageUrl(int publishmentSystemId, ETemplateType templateType)
        {
            return PageUtils.GetCmsUrl(nameof(PageProgressBar), new NameValueCollection
            {
                {"publishmentSystemId", publishmentSystemId.ToString()},
                {"templateType", ETemplateTypeUtils.GetValue(templateType)},
                {"deleteAllPage", true.ToString()}
            });
        }

        public void Page_Load(object sender, EventArgs e)
        {
            if (IsForbidden) return;

            PageUtils.CheckRequestParameter("publishmentSystemId");

            var userKeyPrefix = Body.GetQueryString("userKeyPrefix");

            if (Body.IsQueryExists("createPublishmentSystem"))
            {
                LtlTitle.Text = "新建站点";
                var pars = AjaxCreateService.GetCreatePublishmentSystemParameters(PublishmentSystemId, Body.GetQueryBool("isImportContents"), Body.GetQueryBool("isImportTableStyles"), Body.GetQueryString("siteTemplateDir"), Body.GetQueryString("onlineTemplateName"), Body.GetQueryBool("isUseTables"), userKeyPrefix);
                LtlRegisterScripts.Text = AjaxManager.RegisterProgressTaskScript(AjaxCreateService.GetCreatePublishmentSystemUrl(), pars, userKeyPrefix, AjaxCreateService.GetCountArrayUrl(), true);
            }
            else if (Body.IsQueryExists("backup") && Body.IsQueryExists("backupType"))
            {
                LtlTitle.Text = "数据备份";

                var parameters =
                    AjaxBackupService.GetBackupParameters(PublishmentSystemId, Body.GetQueryString("backupType"), userKeyPrefix);
                LtlRegisterScripts.Text = AjaxManager.RegisterWaitingTaskScript(AjaxBackupService.GetBackupUrl(), parameters);
            }
            else if (Body.IsQueryExists("recovery") && Body.IsQueryExists("isZip"))
            {
                LtlTitle.Text = "数据恢复";
                var parameters = AjaxBackupService.GetRecoveryParameters(PublishmentSystemId,
                    Body.GetQueryBool("isDeleteChannels"), Body.GetQueryBool("isDeleteTemplates"),
                    Body.GetQueryBool("isDeleteFiles"), Body.GetQueryBool("isZip"),
                    PageUtils.UrlEncode(Body.GetQueryString("path")), Body.GetQueryBool("isOverride"),
                    Body.GetQueryBool("isUseTable"), userKeyPrefix);
                LtlRegisterScripts.Text = AjaxManager.RegisterWaitingTaskScript(AjaxBackupService.GetRecoveryUrl(), parameters);
            }
            else if (Body.IsQueryExists("deleteAllPage") && Body.IsQueryExists("templateType"))
            {
                DeleteAllPage();
            }
            else if (Body.IsQueryExists("createIndex"))
            {
                CreateIndex();
            }
        }

        //生成首页
        private void CreateIndex()
        {
            LtlTitle.Text = "生成首页";
            var link = new HyperLink
            {
                NavigateUrl = PageUtility.GetIndexPageUrl(PublishmentSystemInfo, false),
                Text = "浏览"
            };
            if (link.NavigateUrl != PageUtils.UnclickedUrl)
            {
                link.Target = "_blank";
            }
            link.Style.Add("text-decoration", "underline");
            try
            {
                CreateManager.CreateChannel(PublishmentSystemId, PublishmentSystemId);
                //FileSystemObject FSO = new FileSystemObject(base.PublishmentSystemID);

                //FSO.AddIndexToWaitingCreate();

                LtlRegisterScripts.Text = @"
<script>
$(document).ready(function(){
    writeResult('首页生成成功。', '');
})
</script>
";
            }
            catch (Exception ex)
            {
                LtlRegisterScripts.Text = $@"
<script>
$(document).ready(function(){{
    writeResult('', '{ex.Message}');
}})
</script>
";
            }
        }

        private void DeleteAllPage()
        {
            var templateType = ETemplateTypeUtils.GetEnumType(Body.GetQueryString("TemplateType"));

            if (templateType == ETemplateType.ChannelTemplate)
            {
                LtlTitle.Text = "删除已生成的栏目页文件";
            }
            else if (templateType == ETemplateType.ContentTemplate)
            {
                LtlTitle.Text = "删除所有已生成的内容页文件";
            }
            else if (templateType == ETemplateType.FileTemplate)
            {
                LtlTitle.Text = "删除所有已生成的文件页";
            }

            try
            {
                if (templateType == ETemplateType.ChannelTemplate)
                {
                    var nodeIdList = DataProvider.NodeDao.GetNodeIdListByPublishmentSystemId(PublishmentSystemId);
                    DirectoryUtility.DeleteChannelsByPage(PublishmentSystemInfo, nodeIdList);
                }
                else if (templateType == ETemplateType.ContentTemplate)
                {
                    var nodeIdList = DataProvider.NodeDao.GetNodeIdListByPublishmentSystemId(PublishmentSystemId);
                    DirectoryUtility.DeleteContentsByPage(PublishmentSystemInfo, nodeIdList);
                }
                else if (templateType == ETemplateType.FileTemplate)
                {
                    DirectoryUtility.DeleteFiles(PublishmentSystemInfo, DataProvider.TemplateDao.GetTemplateIdListByType(PublishmentSystemId, ETemplateType.FileTemplate));
                }

                Body.AddSiteLog(PublishmentSystemId, LtlTitle.Text);

                LtlRegisterScripts.Text = @"
<script>
$(document).ready(function(){
    writeResult('任务执行成功。', '');
})
</script>
";
            }
            catch (Exception ex)
            {
                LtlRegisterScripts.Text = $@"
<script>
$(document).ready(function(){{
    writeResult('', '{ex.Message}');
}})
</script>
";
            }
        }
    }
}
