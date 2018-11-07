/*
Menu settings for the mod. 
Menu code regurgitated from the game code
Settings loaded from this.Config file and changes saved onto this.Config file.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NPCMapLocations
{
	public class ModMenu : IClickableMenu
	{
		private readonly ModConfig Config;
		private readonly ClickableTextureComponent downArrow;
		private readonly IModHelper Helper;
		private readonly MapModButton immersionButton1;
		private readonly MapModButton immersionButton2;
		private readonly MapModButton immersionButton3;
		private readonly Texture2D map;
		private readonly int mapX;
		private readonly int mapY;
		private readonly ClickableTextureComponent okButton;
		private readonly List<OptionsElement> options = new List<OptionsElement>();
		private readonly List<ClickableComponent> optionSlots = new List<ClickableComponent>();
		private readonly ClickableTextureComponent scrollBar;
		private readonly Rectangle scrollBarRunner;
		private readonly ClickableTextureComponent upArrow;
		private bool canClose;
		private int currentItemIndex;
		private int optionsSlotHeld = -1;
		private bool scrolling;

		public ModMenu(
			Dictionary<string, bool> secondaryNpcs,
			Dictionary<string, string> npcNames,
			Dictionary<string, int> MarkerCropOffsets,
			IModHelper helper,
			ModConfig config
		) : base(Game1.viewport.Width / 2 - (1100 + borderWidth * 2) / 2,
			Game1.viewport.Height / 2 - (725 + borderWidth * 2) / 2,
			1100 + borderWidth * 2,
			650 + borderWidth * 2, false)
		{
			Helper = helper;
			Config = config;
			map = Game1.content.Load<Texture2D>("LooseSprites\\map");
			var topLeftPositionForCenteringOnScreen =
				Utility.getTopLeftPositionForCenteringOnScreen(map.Bounds.Width * Game1.pixelZoom, 180 * Game1.pixelZoom,
					0, 0);
			mapX = (int) topLeftPositionForCenteringOnScreen.X;
			mapY = (int) topLeftPositionForCenteringOnScreen.Y;
			okButton = new ClickableTextureComponent("OK",
				new Rectangle(xPositionOnScreen + width - Game1.tileSize * 2,
					yPositionOnScreen + height - 7 * Game1.tileSize / 4, Game1.tileSize, Game1.tileSize), null, null,
				Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
			upArrow = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + Game1.tileSize / 4,
					yPositionOnScreen + Game1.tileSize,
					11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(421, 459, 11, 12),
				Game1.pixelZoom);
			downArrow = new ClickableTextureComponent(
				new Rectangle(xPositionOnScreen + width + Game1.tileSize / 4,
					yPositionOnScreen + height - Game1.tileSize, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom),
				Game1.mouseCursors, new Rectangle(421, 472, 11, 12), Game1.pixelZoom);
			scrollBar = new ClickableTextureComponent(
				new Rectangle(upArrow.bounds.X + Game1.pixelZoom * 3,
					upArrow.bounds.Y + upArrow.bounds.Height + Game1.pixelZoom, 6 * Game1.pixelZoom,
					10 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), Game1.pixelZoom);
			scrollBarRunner = new Rectangle(scrollBar.bounds.X,
				upArrow.bounds.Y + upArrow.bounds.Height + Game1.pixelZoom, scrollBar.bounds.Width,
				height - Game1.tileSize * 2 - upArrow.bounds.Height - Game1.pixelZoom * 2);
			for (var i = 0; i < 7; i++)
				optionSlots.Add(new ClickableComponent(
					new Rectangle(xPositionOnScreen + Game1.tileSize / 4,
						yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * ((height - Game1.tileSize * 2) / 7),
						width - Game1.tileSize / 2, (height - Game1.tileSize * 2) / 7 + Game1.pixelZoom), string.Concat(i)));

			// Translate labels and initialize buttons to handle button press
			string minimapLabel = helper.Translation.Get("minimap.label");
			string immersionLabel = helper.Translation.Get("immersion.label");
			string villagersLabel = helper.Translation.Get("villagers.label");
			immersionButton1 = new MapModButton("immersion.option1", 4, -1, -1, -1, -1, helper, config);
			immersionButton2 = new MapModButton("immersion.option2", 5, -1, -1, -1, -1, helper, config);
			immersionButton3 = new MapModButton("immersion.option3", 6, -1, -1, -1, -1, helper, config);

			//this.options.Add(new OptionsElement("Menu Key:"));
			//this.options.Add(new MapModInputListener("Change menu key", 37, this.optionSlots[0].bounds.Width, -1, -1));
			options.Add(new OptionsElement("NPC Map Locations"));

			var widths = new List<int>();
			for (var i = 0; i < 16; i++)
				widths.Add(75 + i * 15);

			var heights = new List<int>();
			for (var j = 0; j < 10; j++)
				heights.Add(45 + j * 15);

			var percentages = new List<int>();
			for (var k = 0; k < 11; k++)
				percentages.Add(50 + k * 5);

			options.Add(new OptionsElement(minimapLabel));
			options.Add(new ModCheckbox("minimap.option1", 54, -1, -1, npcNames, MarkerCropOffsets, helper, config));
			options.Add(new ModPlusMinus("minimap.plusMinus1", 55, widths, helper, config));
			options.Add(new ModPlusMinus("minimap.plusMinus2", 56, heights, helper, config));
			//options.Add(new ModPlusMinus("minimap.plusMinus3", 57, percentages, helper, config));

			options.Add(new OptionsElement(immersionLabel));
			options.Add(immersionButton1);
			options.Add(immersionButton2);
			options.Add(immersionButton3);
			options.Add(new ModCheckbox("immersion.option4", 49, -1, -1, npcNames, MarkerCropOffsets, helper,
				config));
			options.Add(new ModCheckbox("immersion.option5", 50, -1, -1, npcNames, MarkerCropOffsets, helper,
				config));
			options.Add(new MapModSlider("immersion.slider1", 0, -1, -1, 0, 12, helper, config));
			options.Add(new MapModSlider("immersion.slider2", 1, -1, -1, 0, 12, helper, config));

			options.Add(new ModCheckbox("extra.option1", 51, -1, -1, npcNames, MarkerCropOffsets, helper, config));
			options.Add(new ModCheckbox("extra.option2", 52, -1, -1, npcNames, MarkerCropOffsets, helper, config));
			options.Add(new ModCheckbox("extra.option3", 53, -1, -1, npcNames, MarkerCropOffsets, helper, config));
			options.Add(new OptionsElement(villagersLabel));

			// Villagers + up to 10 custom NPCs
			var orderedNames = npcNames.Keys.ToList();
			orderedNames.Sort();
			var idx = 7;
			foreach (var name in orderedNames)
				if (secondaryNpcs.ContainsKey(name))
					if (secondaryNpcs[name])
						options.Add(new ModCheckbox(name, idx++, -1, -1, npcNames, MarkerCropOffsets, helper, config));
					else
						idx++;
				else
					options.Add(new ModCheckbox(name, idx++, -1, -1, npcNames, MarkerCropOffsets, helper, config));
		}

		// Override snappy controls on controller
		public override bool overrideSnappyMenuCursorMovementBan()
		{
			return true;
		}

		private void SetScrollBarToCurrentIndex()
		{
			if (options.Any())
			{
				scrollBar.bounds.Y =
					scrollBarRunner.Height / Math.Max(1, options.Count - 7 + 1) * currentItemIndex +
					upArrow.bounds.Bottom + Game1.pixelZoom;
				if (currentItemIndex == options.Count() - 7)
					scrollBar.bounds.Y = downArrow.bounds.Y - scrollBar.bounds.Height - Game1.pixelZoom;
			}
		}

		public override void leftClickHeld(int x, int y)
		{
			if (GameMenu.forcePreventClose) return;

			base.leftClickHeld(x, y);
			if (scrolling)
			{
				var y2 = scrollBar.bounds.Y;
				scrollBar.bounds.Y =
					Math.Min(
						yPositionOnScreen + height - Game1.tileSize - Game1.pixelZoom * 3 - scrollBar.bounds.Height,
						Math.Max(y, yPositionOnScreen + upArrow.bounds.Height + Game1.pixelZoom * 5));
				var num = (y - scrollBarRunner.Y) / (float) scrollBarRunner.Height;
				currentItemIndex = Math.Min(options.Count - 7, Math.Max(0, (int) (options.Count * num)));
				SetScrollBarToCurrentIndex();
				if (y2 != scrollBar.bounds.Y) Game1.playSound("shiny4");
			}
			else
			{
				if (optionsSlotHeld == -1 || optionsSlotHeld + currentItemIndex >= options.Count) return;

				options[currentItemIndex + optionsSlotHeld].leftClickHeld(
					x - optionSlots[optionsSlotHeld].bounds.X, y - optionSlots[optionsSlotHeld].bounds.Y);
			}
		}

		public override void receiveKeyPress(Keys key)
		{
			if ((Game1.options.menuButton.Contains(new InputButton(key)) ||
			     Game1.options.doesInputListContain(Game1.options.mapButton, key)) && readyToClose() && canClose)
			{
				exitThisMenu(true);
				return;
			}

			if (key.ToString().Equals(Config.MenuKey) && readyToClose() && canClose)
			{
				Game1.exitActiveMenu();
				Game1.activeClickableMenu = new GameMenu();
				(Game1.activeClickableMenu as GameMenu).changeTab(GameMenu.mapTab);
				return;
			}

			canClose = true;
			if (optionsSlotHeld == -1 || optionsSlotHeld + currentItemIndex >= options.Count) return;

			options[currentItemIndex + optionsSlotHeld].receiveKeyPress(key);
		}

		public override void receiveScrollWheelAction(int direction)
		{
			if (GameMenu.forcePreventClose) return;

			base.receiveScrollWheelAction(direction);
			if (direction > 0 && currentItemIndex > 0)
			{
				UpArrowPressed();
				Game1.playSound("shiny4");
				return;
			}

			if (direction < 0 && currentItemIndex < Math.Max(0, options.Count() - 7))
			{
				DownArrowPressed();
				Game1.playSound("shiny4");
			}
		}

		public override void releaseLeftClick(int x, int y)
		{
			if (GameMenu.forcePreventClose) return;

			base.releaseLeftClick(x, y);
			if (optionsSlotHeld != -1 && optionsSlotHeld + currentItemIndex < options.Count)
				options[currentItemIndex + optionsSlotHeld].leftClickReleased(
					x - optionSlots[optionsSlotHeld].bounds.X, y - optionSlots[optionsSlotHeld].bounds.Y);

			optionsSlotHeld = -1;
			scrolling = false;
		}

		private void DownArrowPressed()
		{
			downArrow.scale = downArrow.baseScale;
			currentItemIndex++;
			SetScrollBarToCurrentIndex();
		}

		private void UpArrowPressed()
		{
			upArrow.scale = upArrow.baseScale;
			currentItemIndex--;
			SetScrollBarToCurrentIndex();
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (GameMenu.forcePreventClose) return;

			if (downArrow.containsPoint(x, y) &&
			    currentItemIndex < Math.Max(0, options.Count() - 7))
			{
				DownArrowPressed();
				Game1.playSound("shwip");
			}
			else if (upArrow.containsPoint(x, y) && currentItemIndex > 0)
			{
				UpArrowPressed();
				Game1.playSound("shwip");
			}
			else if (scrollBar.containsPoint(x, y))
			{
				scrolling = true;
			}
			else if (!downArrow.containsPoint(x, y) && x > xPositionOnScreen + width &&
			         x < xPositionOnScreen + width + Game1.tileSize * 2 && y > yPositionOnScreen &&
			         y < yPositionOnScreen + height)
			{
				scrolling = true;
				leftClickHeld(x, y);
			}
			else if (immersionButton1.rect.Contains(x, y))
			{
				immersionButton1.receiveLeftClick(x, y);
				immersionButton2.GreyOut();
				immersionButton3.GreyOut();
			}
			else if (immersionButton2.rect.Contains(x, y))
			{
				immersionButton2.receiveLeftClick(x, y);
				immersionButton1.GreyOut();
				immersionButton3.GreyOut();
			}
			else if (immersionButton3.rect.Contains(x, y))
			{
				immersionButton3.receiveLeftClick(x, y);
				immersionButton1.GreyOut();
				immersionButton2.GreyOut();
			}

			if (okButton.containsPoint(x, y))
			{
				okButton.scale -= 0.25f;
				okButton.scale = Math.Max(0.75f, okButton.scale);
				(Game1.activeClickableMenu as ModMenu).exitThisMenu(false);
				Game1.activeClickableMenu = new GameMenu();
				(Game1.activeClickableMenu as GameMenu).changeTab(3);
			}

			y -= 15;
			currentItemIndex = Math.Max(0, Math.Min(options.Count() - 7, currentItemIndex));
			for (var i = 0; i < optionSlots.Count(); i++)
				if (optionSlots[i].bounds.Contains(x, y) &&
				    currentItemIndex + i < options.Count() && options[currentItemIndex + i]
					    .bounds.Contains(x - optionSlots[i].bounds.X, y - optionSlots[i].bounds.Y))
				{
					options[currentItemIndex + i]
						.receiveLeftClick(x - optionSlots[i].bounds.X, y - optionSlots[i].bounds.Y);
					optionsSlotHeld = i;
					break;
				}
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
		}

		public override void performHoverAction(int x, int y)
		{
			if (GameMenu.forcePreventClose) return;

			if (okButton.containsPoint(x, y))
			{
				okButton.scale = Math.Min(okButton.scale + 0.02f, okButton.baseScale + 0.1f);
				return;
			}

			okButton.scale = Math.Max(okButton.scale - 0.02f, okButton.baseScale);
			upArrow.tryHover(x, y, 0.1f);
			downArrow.tryHover(x, y, 0.1f);
			scrollBar.tryHover(x, y, 0.1f);
		}

		// Draw menu
		public override void draw(SpriteBatch b)
		{
			if (Game1.options.showMenuBackground) drawBackground(b);

			Game1.drawDialogueBox(mapX - Game1.pixelZoom * 8, mapY - Game1.pixelZoom * 24,
				(map.Bounds.Width + 16) * Game1.pixelZoom, 212 * Game1.pixelZoom, false, true, null, false);
			b.Draw(map, new Vector2(mapX, mapY), new Rectangle(0, 0, 300, 180),
				Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.86f);
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true, null,
				false);
			okButton.draw(b);
			var buttonWidth = (int) Game1.dialogueFont.MeasureString(Helper.Translation.Get("immersion.option3")).X;
			if (!GameMenu.forcePreventClose)
			{
				upArrow.draw(b);
				downArrow.draw(b);

				if (options.Count() > 7)
				{
					drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), scrollBarRunner.X,
						scrollBarRunner.Y, scrollBarRunner.Width, scrollBarRunner.Height, Color.White,
						Game1.pixelZoom, false);
					scrollBar.draw(b);
				}

				for (var i = 0; i < optionSlots.Count(); i++)
				{
					var x = optionSlots[i].bounds.X;
					var y = optionSlots[i].bounds.Y + Game1.tileSize / 4;
					if (currentItemIndex >= 0 && currentItemIndex + i < options.Count())
					{
						if (options[currentItemIndex + i] is MapModButton)
						{
							var bounds = new Rectangle(x + 28, y, buttonWidth + Game1.tileSize + 8, Game1.tileSize + 8);
							switch (options[currentItemIndex + i].whichOption)
							{
								case 4:
									immersionButton1.rect = bounds;
									break;
								case 5:
									immersionButton2.rect = bounds;
									break;
								case 6:
									immersionButton3.rect = bounds;
									break;
							}

							drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), bounds.X, bounds.Y,
								bounds.Width, bounds.Height, Color.White * (options[currentItemIndex + i].greyedOut ? 0.33f : 1f),
								1f, false);
						}

						if (currentItemIndex + i == 0)
							Utility.drawTextWithShadow(b, "NPC Map Locations", Game1.dialogueFont,
								new Vector2(x + Game1.tileSize / 2, y + Game1.tileSize / 4), Color.Black);
						else
							options[currentItemIndex + i].draw(b, x, y);
					}
				}
			}

			if (!Game1.options.hardwareCursor)
				b.Draw(Game1.mouseCursors, new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY()),
					Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors,
						Game1.options.gamepadControls ? 44 : 0, 16, 16), Color.White, 0f, Vector2.Zero,
					Game1.pixelZoom + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
		}
	}

	// Mod button for the three main modes
	internal class MapModButton : OptionsElement
	{
		private readonly ModConfig Config;
		public bool isActive;
		public Rectangle rect;

		public MapModButton(
			string label,
			int whichOption,
			int x,
			int y,
			int width,
			int height,
			IModHelper helper,
			ModConfig config
		) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
		{
			Config = config;
			this.label = helper.Translation.Get(label);
			rect = new Rectangle(x, y, width, height);

			if (config.ImmersionOption == whichOption - 3)
				greyedOut = false;
			else
				greyedOut = true;
		}

		public override void receiveLeftClick(int x, int y)
		{
			if (!isActive)
				if (whichOption > 3)
				{
					Game1.playSound("drumkit6");
					base.receiveLeftClick(x, y);
					isActive = true;
					greyedOut = false;
					Config.ImmersionOption = whichOption - 3;
				}
		}

		public void GreyOut()
		{
			isActive = false;
			greyedOut = true;
		}

		public override void draw(SpriteBatch b, int slotX, int slotY)
		{
			base.draw(b, slotX - 32, slotY);
		}
	}

	// Mod checkbox for settings and npc blacklst
	internal class ModCheckbox : OptionsElement
	{
		private readonly ModConfig Config;
		private readonly Dictionary<string, string> CustomNames;
		private readonly IModHelper Helper;
		private readonly Dictionary<string, int> MarkerCropOffsets;
		private readonly List<string> orderedNames;
		public bool isChecked;

		public ModCheckbox(
			string label,
			int whichOption,
			int x,
			int y,
			Dictionary<string, string> npcNames,
			Dictionary<string, int> MarkerCropOffsets,
			IModHelper helper,
			ModConfig config
		) : base(label, x, y, 9 * Game1.pixelZoom, 9 * Game1.pixelZoom, whichOption)
		{
			Helper = helper;
			Config = config;
			this.MarkerCropOffsets = MarkerCropOffsets;
			CustomNames = npcNames;
			this.label = label;

			if (whichOption < 49)
			{
				orderedNames = npcNames.Keys.ToList();
				orderedNames.Sort();

				if (whichOption > 6 && whichOption < 49)
				{
					isChecked = !Config.NpcBlacklist.Contains(orderedNames[whichOption - 7]);
					return;
				}
			}
			else if (whichOption > 48)
			{
				this.label = Helper.Translation.Get(label);
			}

			switch (whichOption)
			{
				case 49:
					isChecked = Config.OnlySameLocation;
					return;
				case 50:
					isChecked = Config.ByHeartLevel;
					return;
				case 51:
					isChecked = Config.MarkQuests;
					return;
				case 52:
					isChecked = Config.ShowHiddenVillagers;
					return;
				case 53:
					isChecked = Config.ShowTravelingMerchant;
					return;
				case 54:
					isChecked = Config.ShowMinimap;
					return;
				default:
					return;
			}
		}

		public override void receiveLeftClick(int x, int y)
		{
			if (greyedOut) return;

			//Game1.soundBank.PlayCue("drumkit6");
			base.receiveLeftClick(x, y);
			isChecked = !isChecked;
			var whichOption = this.whichOption;

			if (whichOption > 6 && whichOption < 49)
			{
				if (isChecked)
					Config.NpcBlacklist.Remove(orderedNames[whichOption - 7]);
				else
					Config.NpcBlacklist.Add(orderedNames[whichOption - 7]);
			}
			else
			{
				switch (whichOption)
				{
					case 49:
						Config.OnlySameLocation = isChecked;
						break;
					case 50:
						Config.ByHeartLevel = isChecked;
						break;
					case 51:
						Config.MarkQuests = isChecked;
						break;
					case 52:
						Config.ShowHiddenVillagers = isChecked;
						break;
					case 53:
						Config.ShowTravelingMerchant = isChecked;
						break;
					case 54:
						Config.ShowMinimap = isChecked;
						break;
					default:
						break;
				}
			}

			Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
		}

		public override void draw(SpriteBatch b, int slotX, int slotY)
		{
			b.Draw(Game1.mouseCursors, new Vector2(slotX + bounds.X, slotY + bounds.Y),
				isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked,
				Color.White * (greyedOut ? 0.33f : 1f), 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None,
				0.4f);
			if (whichOption > 6 && whichOption < 49)
			{
				var npc = Game1.getCharacterFromName(label);
				if (npc == null) return;

				if (isChecked)
					Game1.spriteBatch.Draw(npc.Sprite.Texture, new Vector2((float) slotX + bounds.X + 50, slotY),
						new Rectangle(0, MarkerCropOffsets[npc.Name], 16, 15), Color.White, 0f, Vector2.Zero,
						Game1.pixelZoom, SpriteEffects.None, 0.4f);
				else
					Game1.spriteBatch.Draw(npc.Sprite.Texture, new Vector2((float) slotX + bounds.X + 50, slotY),
						new Rectangle(0, MarkerCropOffsets[npc.Name], 16, 15), Color.White * 0.33f, 0f, Vector2.Zero,
						Game1.pixelZoom, SpriteEffects.None, 0.4f);

				// Draw names
				slotX += 75;
				if (whichOption == -1)
					SpriteText.drawString(b, label, slotX + bounds.X, slotY + bounds.Y + 12, 999, -1, 999, 1f,
						0.1f, false, -1, "", -1);
				else
					Utility.drawTextWithShadow(b, CustomNames[label], Game1.dialogueFont,
						new Vector2(slotX + bounds.X + bounds.Width + 8, slotY + bounds.Y),
						greyedOut ? Game1.textColor * 0.33f : Game1.textColor, 1f, 0.1f, -1, -1, 1f, 3);
			}
			else
			{
				base.draw(b, slotX, slotY);
			}
		}
	}

	// Mod slider for heart level this.Config
	internal class MapModSlider : OptionsElement
	{
		private readonly ModConfig Config;
		private readonly IModHelper Helper;
		private readonly int max;
		private int min;
		private float value;
		public string valueLabel;

		public MapModSlider(
			string label,
			int whichOption,
			int x,
			int y,
			int min,
			int max,
			IModHelper helper,
			ModConfig config
		) : base(label, x, y, 48 * Game1.pixelZoom, 6 * Game1.pixelZoom, whichOption)
		{
			this.min = min;
			this.max = max;
			if (whichOption != 0 && whichOption != 1) bounds.Width = bounds.Width * 2;
			Helper = helper;
			Config = config;
			valueLabel = helper.Translation.Get(label);

			switch (whichOption)
			{
				case 0:
					value = Config.HeartLevelMin;
					break;
				case 1:
					value = Config.HeartLevelMax;
					break;
				default:
					break;
			}
		}

		public override void leftClickHeld(int x, int y)
		{
			if (greyedOut) return;

			base.leftClickHeld(x, y);
			value = x >= bounds.X
				? (x <= bounds.Right - 10 * Game1.pixelZoom
					? (int) ((x - bounds.X) / (float) (bounds.Width - 10 * Game1.pixelZoom) *
					         max)
					: max)
				: 0;

			switch (whichOption)
			{
				case 0:
					Config.HeartLevelMin = (int) value;
					break;
				case 1:
					Config.HeartLevelMax = (int) value;
					break;
				default:
					break;
			}

			Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
		}

		public override void receiveLeftClick(int x, int y)
		{
			if (greyedOut) return;

			base.receiveLeftClick(x, y);
			leftClickHeld(x, y);
		}

		public override void draw(SpriteBatch b, int slotX, int slotY)
		{
			label = valueLabel + ": " + value;
			greyedOut = false;
			if (whichOption == 0 || whichOption == 1) greyedOut = !Config.ByHeartLevel;

			base.draw(b, slotX, slotY);
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, OptionsSlider.sliderBGSource, slotX + bounds.X,
				slotY + bounds.Y, bounds.Width, bounds.Height, Color.White, Game1.pixelZoom, false);
			b.Draw(Game1.mouseCursors,
				new Vector2(
					slotX + bounds.X + (bounds.Width - 10 * Game1.pixelZoom) *
					(value / max), slotY + bounds.Y),
				OptionsSlider.sliderButtonRect, Color.White, 0f, Vector2.Zero, Game1.pixelZoom,
				SpriteEffects.None, 0.9f);
		}
	}

	public class ModPlusMinus : OptionsElement
	{
		public static bool snapZoomPlus;

		public static bool snapZoomMinus;

		public static Rectangle minusButtonSource = new Rectangle(177, 345, 7, 8);

		public static Rectangle plusButtonSource = new Rectangle(184, 345, 7, 8);
		private readonly ModConfig Config;

		public List<string> displayOptions;
		private readonly IModHelper Helper;

		private Rectangle minusButton;
		public List<int> options;

		private Rectangle plusButton;
		private int txtSize;

		public int selected;

		public ModPlusMinus(string label, int whichOption, List<int> options, IModHelper helper,
			ModConfig config, int x = -1, int y = -1)
			: base(label, x, y, 28, 28, whichOption)
		{
			Config = config;
			Helper = helper;
			this.options = options;
			displayOptions = new List<string>();

			if (x == -1) x = 32;
			if (y == -1) y = 16;

			txtSize = (int) Game1.dialogueFont.MeasureString($"options[0]").X + 28;
			var addString = whichOption == 57 ? "%" : "px";
			foreach (var displayOption in options)
			{
				txtSize = Math.Max((int) Game1.dialogueFont.MeasureString($"{displayOption}{addString}").X + 28, txtSize);
				displayOptions.Add($"{displayOption}{addString}");
			}

			bounds = new Rectangle(x, y, (int) (1.5 * txtSize), 32);
			this.label = helper.Translation.Get(label);
			this.whichOption = whichOption;
			minusButton = new Rectangle(x, 16, 28, 32);
			plusButton = new Rectangle(bounds.Right - 96, 16, 28, 32);

			switch (whichOption)
			{
				case 55:
					selected = (int) MathHelper.Clamp(((int) Math.Floor((Config.MinimapWidth - 75) / 15.0)), 0,
						options.Count - 1);
					options[selected] = Config.MinimapWidth;
					break;
				case 56:
					selected = (int) MathHelper.Clamp(((int) Math.Floor((Config.MinimapHeight - 45) / 15.0)), 0,
						options.Count - 1);
					options[selected] = Config.MinimapHeight;
					break;
				/*
				case 57:
					selected = (int)MathHelper.Clamp(((int)Math.Floor((Config.MinimapOpacity - 50) / 5.0)), 0, options.Count - 1);
					options[selected] = Config.MinimapOpacity;
					break;
				*/
				default:
					break;
			}
		}

		public override void receiveLeftClick(int x, int y)
		{
			if (!greyedOut && options.Count > 0)
			{
				var num = selected;
				if (minusButton.Contains(x, y) && selected != 0)
				{
					selected--;
					snapZoomMinus = true;
					Game1.playSound("drumkit6");
				}
				else if (plusButton.Contains(x, y) && selected != options.Count - 1)
				{
					selected++;
					snapZoomPlus = true;
					Game1.playSound("drumkit6");
				}

				if (selected < 0)
					selected = 0;
				else if (selected >= options.Count) selected = options.Count - 1;
			}

			switch (whichOption)
			{
				case 55:
					Config.MinimapWidth = options[selected];
					break;
				case 56:
					Config.MinimapHeight = options[selected];
					break;
      /*
      case 57:
      Config.MinimapOpacity = options[selected];
      break;
      */
				default:
					break;
			}

			Helper.Data.WriteJsonFile($"config/{Constants.SaveFolderName}.json", Config);
		}

		public override void receiveKeyPress(Keys key)
		{
			base.receiveKeyPress(key);
			if (Game1.options.snappyMenus && Game1.options.gamepadControls)
			{
				if (Game1.options.doesInputListContain(Game1.options.moveRightButton, key))
					receiveLeftClick(plusButton.Center.X, plusButton.Center.Y);
				else if (Game1.options.doesInputListContain(Game1.options.moveLeftButton, key))
					receiveLeftClick(minusButton.Center.X, minusButton.Center.Y);
			}
		}

		public override void draw(SpriteBatch b, int slotX, int slotY)
		{
			greyedOut = !Config.ShowMinimap;
			b.Draw(Game1.mouseCursors, new Vector2(slotX + minusButton.X, slotY + minusButton.Y), minusButtonSource,
				Color.White * (greyedOut ? 0.33f : 1f) * (selected == 0 ? 0.5f : 1f), 0f, Vector2.Zero, 4f, SpriteEffects.None,
				0.4f);
			b.DrawString(Game1.dialogueFont,
				selected < displayOptions.Count && selected != -1 ? displayOptions[selected] : "",
				new Vector2((int) (txtSize / 2) + slotX, slotY + minusButton.Y), Game1.textColor * (greyedOut ? 0.33f : 1f));
			b.Draw(Game1.mouseCursors, new Vector2(slotX + plusButton.X, slotY + plusButton.Y), plusButtonSource,
				Color.White * (greyedOut ? 0.33f : 1f) * (selected == displayOptions.Count - 1 ? 0.5f : 1f), 0f, Vector2.Zero,
				4f, SpriteEffects.None, 0.4f);
			if (!Game1.options.snappyMenus && Game1.options.gamepadControls)
			{
				if (snapZoomMinus)
				{
					Game1.setMousePosition(slotX + minusButton.Center.X, slotY + minusButton.Center.Y);
					snapZoomMinus = false;
				}
				else if (snapZoomPlus)
				{
					Game1.setMousePosition(slotX + plusButton.Center.X, slotY + plusButton.Center.Y);
					snapZoomPlus = false;
				}
			}

			base.draw(b, slotX, slotY);
		}
	}
}