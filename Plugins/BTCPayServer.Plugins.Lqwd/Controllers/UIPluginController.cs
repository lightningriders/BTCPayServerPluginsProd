using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Lqwd.Data;
using BTCPayServer.Plugins.Lqwd.Services;
using BTCPayServer.Data;
using BTCPayServer.Filters;
using BTCPayServer.ModelBinders;
using BTCPayServer.Models;
using BTCPayServer.Payments;
using BTCPayServer.Security.Greenfield;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Rates;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Lightning;
using System.Linq;
using NBitcoin;

using BTCPayServer.Plugins.Lqwd.ViewModels;

namespace BTCPayServer.Plugins.Lqwd
{
    [Route("stores/{storeId}/plugins/lqwd")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewProfile)]
    public class UIPluginController : Controller
    {
        private readonly LqwdPluginService _pluginService;

        public UIPluginController(LqwdPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        // Load API data when the page is first loaded
        public async Task<IActionResult> Index(string storeId)
        {
            var data = await _pluginService.FetchLiveApiData();
            var orders = await _pluginService.GetOrders(storeId) ?? new List<OrderData>();
            var channels = await _pluginService.GetChannels(storeId);
            return View(new PluginPageViewModel { ApiResponse = data, Orders = orders, Channels = channels });
        }

        [HttpGet("settings")]
        public async Task<IActionResult> Settings(string storeId)
        {
            var (settings, activeLsps) = await _pluginService.GetStoreSettingsWithDefaults(storeId);

            var viewModel = new SettingsViewModel
            {
                Settings = settings,
                ActiveLsps = activeLsps
            };

            return View(viewModel);
        }

        [HttpGet("info")]
        public IActionResult Info(string storeId)
        {
            return View("MissingLnd");
        }

        [HttpGet("channels")]
        public async Task<IActionResult> Channels(string storeId)
        {
            ViewData["ActivePage"] = "Channels";

            var lspPubKeys = await _pluginService.GetActiveLspPubKeys();

            // Get channels (BTCPayServer.Lightning.LightningChannel[])
            LightningChannel[] allChannels = await _pluginService.GetChannels(storeId);

            var filteredChannels = new List<LightningChannel>();
            foreach (var channel in allChannels)
            {
                foreach (var lsp in lspPubKeys)
                {
                    if (channel.RemoteNode == lsp)
                    {
                        filteredChannels.Add(channel);
                        break;
                    }
                }
            }

            // Build ViewModel
            var lspChannelInfos = new List<LspChannelsViewModel.LspChannelInfo>();
            long totalLocalSats = 0;

            foreach (var chan in filteredChannels)
            {
                long localSat = (long)chan.LocalBalance.ToUnit(LightMoneyUnit.Satoshi);
                long capacitySat = (long)chan.Capacity.ToUnit(LightMoneyUnit.Satoshi);
                long remoteSat = capacitySat - localSat;
                totalLocalSats += localSat;

                lspChannelInfos.Add(new LspChannelsViewModel.LspChannelInfo
                {
                    ChannelId = chan.ChannelPoint.ToString(),
                    LocalBalance = chan.LocalBalance,
                    RemoteBalance = new LightMoney(remoteSat, LightMoneyUnit.Satoshi)
                });
            }

            var model = new LspChannelsViewModel
            {
                StoreId = storeId,
                CryptoCode = "BTC",
                IsConnectedToLsp = filteredChannels.Count > 0,
                TotalLocalBalance = new LightMoney(totalLocalSats, LightMoneyUnit.Satoshi),
                LspChannels = lspChannelInfos
            };

            return View("Channels", model);
        }



        [HttpPost("settings/activate")]
        public async Task<IActionResult> SetActiveLsps(string storeId, string newKey)
        {
            await _pluginService.SetActiveLsps(storeId, newKey);
            return RedirectToAction("Settings", new { storeId });
        }

        [HttpPost("settings/add")]
        public async Task<IActionResult> AddLspsSetting(string storeId, string name, string url)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
            {
                TempData["Error"] = "Name and URL are required.";
                return RedirectToAction("Settings", new { storeId });
            }

            var key = $"lsps_{name.Trim()}";

            var exists = await _pluginService.DoesSettingExist(storeId, key);
            if (exists)
            {
                TempData["Error"] = $"An LSPS with name '{name}' already exists.";
                return RedirectToAction("Settings", new { storeId });
            }

            await _pluginService.AddSetting(storeId, key, url.Trim());
            return RedirectToAction("Settings", new { storeId });
        }

        [HttpPost("settings/remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLspsSetting(string storeId, string key)
        {
            if (!key.StartsWith("lsps_"))
            {
                TempData["Error"] = "Invalid LSPS key.";
                return RedirectToAction("Settings", new { storeId });
            }

            await _pluginService.RemoveSetting(storeId, key);
            return RedirectToAction("Settings", new { storeId });
        }

        [HttpPost("settings/connect")]
        public async Task<IActionResult> ConnectToLsps(string storeId)
        {
            var messages = await _pluginService.ConnectToActiveLspsUris(storeId);

            // TempData["ConnectMessages"] = messages;
            TempData["ConnectMessages"] = "LSP connection attempt triggered.";
            return RedirectToAction("Index", new { storeId });
        }

        [HttpGet("payview")]
        public IActionResult PayView(string storeId)
        {
            return View("PayView");
        }

        [HttpGet("generate-qr")]
        public IActionResult GenerateQr(string data)
        {
            using var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var png = qrCode.GetGraphic(20);
            return File(png, "image/png");
        }



        // Fetch live API data when button is clicked
        [HttpGet("refresh")]
        public async Task<IActionResult> Refresh(string storeId)
        {
            var data = await _pluginService.FetchLiveApiData();
            return Content(data, "application/json");
        }

        [HttpGet("lnd/islspsconnected")]
        public async Task<IActionResult> IsLspsConnected(string storeId)
        {
            var connected = await _pluginService.IsLspsConnected(storeId);
            return Ok(new { connected }); // ✅ returns: { "connected": true }
        }

        [HttpGet("lnd/identity_pubkey")]
        public async Task<IActionResult> GetMyNodePubKey(string storeId)
        {
            var pubkey = await _pluginService.GetNodePubKey(storeId);
            if (string.IsNullOrEmpty(pubkey))
                return NotFound(new { error = "Pubkey not found or client not supported" });

            return Ok(new { nodePubKey = pubkey });
        }


        [HttpGet("test")]
        public async Task<IActionResult> Test(string storeId)
        {
            var data = await _pluginService.TestData(storeId);

            return Content(data);
        }
        [HttpGet("lndinfo")]
        public async Task<IActionResult> LndInfo(string storeId)
        {
            // var data = await _pluginService.TestData(storeId);
            var data = await _pluginService.GetLndInfoAsJson(storeId);

            return Content(data);
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder(string storeId, [FromBody] OrderRequest order)
        {
            var newOrder = await _pluginService.CreateOrder(storeId, order);

            if (newOrder == null)
            {
                return BadRequest(new { error = "Failed to create order. Please try again later." });
            }

            return Json(newOrder);
        }

        [HttpGet("fetch-status/{orderId}")]
        public async Task<IActionResult> FetchStatus(string storeId, string orderId)
        {
            var order = await _pluginService.FetchOrderStatus(orderId);
            return Json(order);
        }
    }


}