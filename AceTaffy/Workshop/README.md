# 永雏塔菲创意工坊发布

`v0.2.4` 的上传目录已准备好：`AceTaffy/Workshop/v0.2.4/AceTaffyMod/`，其中 `StageModInfo.xml` 位于目录根部。`python AceTaffy/Workshop/prepare_workshop.py` 是新版本的准备命令；它核对发行 ZIP 的 SHA256、解出 `AceTaffyMod` 文件夹、制作 512×512 的 `preview.jpg`、写入 `desc.txt`，并运行包校验。为避免覆盖已审核的候选，暂存目录存在时脚本会停止。

安装的 BaseMod (`2603522001`) 附带 `SteamworkUploader.rar`。解压后，在 Steam 已登录的环境中打开 `SteamworkUploader_cn.exe`。将上述 **AceTaffyMod 文件夹**拖到“新建物品”页，核对标题、标签、介绍和封面，然后上传。上传器会查找目录根部的 `preview.jpg` 和 `desc.txt`；由于其读取顺序可能让介绍未自动填入，提交前必须检查并手动粘贴 `desc.txt` 中的正文。

本次拟用标题：`永雏塔菲：蒸汽来客 v0.2.4`；标签：`Invitation`。说明见暂存目录的 `desc.txt`。上传后记录创意工坊物品 URL/ID，并在工坊页面确认可见性及 BaseMod 必需物品关联。

后续更新在同一工具的“更新物品”页进行：填写原物品的完整 URL (`...?id=<物品ID>`)、拖入新版本文件夹、填写更新日志，再提交。不要用“新建物品”页更新旧模组。

`StageModInfo.xml` 的 `<Version>1.1</Version>` 是邀请模组 XML 格式版本。发布版本写在工坊标题、介绍、包内 README 和发行 ZIP 名称中。`LoRSteamworks.exe` 的当前实现只调用新建物品接口；需要更新时使用上述 BaseMod 上传器。
