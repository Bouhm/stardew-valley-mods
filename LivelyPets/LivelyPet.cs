using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

// Basically just game code with some AI additions
// In the future, much better off rewriting everything
// instead of using game code
namespace LivelyPets
{
  public class LivelyPet : Pet
  {
    public IMonitor Monitor;
    private readonly NetInt netCurrentBehavior = new NetInt();
    private int startedBehavior = -1;
    private bool wasPetToday;
    private int pushingTimer;
    private int skipHorizontal;
    private bool skipHorizontalUp;
    private int durationOfRandomMovements = 0;

    private Farmer activeFarmer= Game1.player;
    private Vector2 prevFarmerPos;
    private int proximity = 1;
    private int pathToFarmerIndex;
    public List<int> pathToFarmer;
    public int pathingIndex;
    public bool isNearFarmer = true;
    private Vector2 start;
    
    public int behaviorTimerMax = 20;
    public int commandBehaviorTimer;
    public string commandBehavior { get; set; }
    private int closenessLevel;
    private int obedienceLevel;

    public int CurrentBehavior
    {
      get
      {
        if (this.isMoving() && commandBehavior == null)
          return 0;
        return (int)((NetFieldBase<int, NetInt>)this.netCurrentBehavior);
      }
      set
      {
        this.netCurrentBehavior.Value = value;
      }
    }

    protected override void initNetFields()
    {
      base.initNetFields();
      this.NetFields.AddFields((INetSerializable)this.netCurrentBehavior);
    }

    public void resetCurrentBehavior()
    {
      //Sprite.StopAnimation();
      //Halt();
    }

    public virtual void setCommandBehavior()
    {
      resetCurrentBehavior();
      commandBehaviorTimer = behaviorTimerMax;
    }

    public override void behaviorOnFarmerLocationEntry(GameLocation location, Farmer who)
    {
      if ((location is Farm || location is FarmHouse && this.CurrentBehavior != 1) && Game1.timeOfDay >= 2000)
      {
        if (this.CurrentBehavior == 1 && !(this.currentLocation is Farm))
          return;
        this.warpToFarmHouse(who);
      }
      else
      {
        if (Game1.timeOfDay >= 2000 || Game1.random.NextDouble() >= 0.5)
          return;
        this.CurrentBehavior = 1;
      }
    }

    public override bool canTalk()
    {
      return false;
    }

    public override void reloadSprite()
    {
      this.DefaultPosition = new Vector2(54f, 8f) * 64f;
      this.HideShadow = true;
      this.Breather = false;
      this.setAtFarmPosition();
    }

    public void warpToFarmHouse(Farmer who)
    {
      FarmHouse homeOfFarmer = Utility.getHomeOfFarmer(who);
      Vector2 vector2 = Vector2.Zero;
      int num = 0;
      for (vector2 = new Vector2((float)Game1.random.Next(2, homeOfFarmer.map.Layers[0].LayerWidth - 3), (float)Game1.random.Next(3, homeOfFarmer.map.Layers[0].LayerHeight - 5)); num < 50 && (!homeOfFarmer.isTileLocationTotallyClearAndPlaceable(vector2) || !homeOfFarmer.isTileLocationTotallyClearAndPlaceable(vector2 + new Vector2(1f, 0.0f)) || homeOfFarmer.isTileOnWall((int)vector2.X, (int)vector2.Y)); ++num)
        vector2 = new Vector2((float)Game1.random.Next(2, homeOfFarmer.map.Layers[0].LayerWidth - 3), (float)Game1.random.Next(3, homeOfFarmer.map.Layers[0].LayerHeight - 4));
      if (num >= 50)
        return;
      Game1.warpCharacter((NPC)this, "FarmHouse", vector2);
      this.CurrentBehavior = 1;
      this.initiateCurrentBehavior();
    }

    public override void dayUpdate(int dayOfMonth)
    {
      this.DefaultPosition = new Vector2(54f, 8f) * 64f;
      this.Sprite.loop = false;
      this.Breather = false;
      if (Game1.isRaining)
      {
        this.CurrentBehavior = 2;
        if (this.currentLocation is Farm)
          this.warpToFarmHouse(Game1.player);
      }
      else if (this.currentLocation is FarmHouse)
        this.setAtFarmPosition();
      if (this.currentLocation is Farm)
      {
        if (this.currentLocation.getTileIndexAt(54, 7, "Buildings") == 1939)
          this.friendshipTowardFarmer = Math.Min(1000, this.friendshipTowardFarmer + 6);
        this.currentLocation.setMapTileIndex(54, 7, 1938, "Buildings", 0);
        this.setTilePosition(54, 8);
        this.position.X -= 64f;
      }
      this.Halt();
      this.CurrentBehavior = 1;
      this.wasPetToday = false;
    }

    public void setAtFarmPosition()
    {
      bool flag = this.currentLocation is Farm;
      if (Game1.isRaining)
        return;
      this.faceDirection(2);
      Game1.warpCharacter((NPC)this, "Farm", new Vector2(54f, 8f));
      this.position.X -= 64f;
    }

    public void warpToFarmer()
    {
      var farmerPos = activeFarmer.getTileLocation();
      var warpPos = farmerPos;
      switch (activeFarmer.FacingDirection)
      {
        case 0:
          // Check right
          if (activeFarmer.currentLocation.isCollidingPosition(
            new Rectangle((int) (farmerPos.X+1)*Game1.tileSize, (int) farmerPos.Y * Game1.tileSize, 64, 64),
            Game1.viewport, this))
          {
            warpPos.X++;
          }
          else
          {
            warpPos.X--;
          }

          break;
        case 1:
          // Check down
          if (activeFarmer.currentLocation.isCollidingPosition(
            new Rectangle((int)farmerPos.X * Game1.tileSize, (int)(farmerPos.Y+1)*Game1.tileSize, 64, 64),
            Game1.viewport, this))
          {
            warpPos.Y++;
          }
          else
          {
            warpPos.Y--;
          }

          break;
        case 2:
          // Check right
          if (activeFarmer.currentLocation.isCollidingPosition(
            new Rectangle((int)(farmerPos.X + 1) * Game1.tileSize, (int)farmerPos.Y * Game1.tileSize, 64, 64),
            Game1.viewport, this))
          {
            warpPos.X++;
          }
          else
          {
            warpPos.X--;
          }

          break;
        case 3:
          // Check down
          if (activeFarmer.currentLocation.isCollidingPosition(
            new Rectangle((int)farmerPos.X * Game1.tileSize, (int)(farmerPos.Y + 1) * Game1.tileSize, 64, 64),
            Game1.viewport, this))
          {
            warpPos.Y++;
          }
          else
          {
            warpPos.Y--;
          }

          break;
      }

      this.faceDirection(activeFarmer.FacingDirection);
      Game1.warpCharacter((NPC)this, activeFarmer.currentLocation.Name, warpPos);
    }

    public override bool shouldCollideWithBuildingLayer(GameLocation location)
    {
      return true;
    }

    public override bool canPassThroughActionTiles()
    {
      return false;
    }

    public override bool checkAction(Farmer who, GameLocation l)
    {
      if (this.wasPetToday)
        return false;
      this.wasPetToday = true;
      this.friendshipTowardFarmer = Math.Min(1000, this.friendshipTowardFarmer + 12);
      if (this.friendshipTowardFarmer >= 1000 && who != null && !who.mailReceived.Contains("petLoveMessage"))
      {
        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Characters:PetLovesYou", (object)this.displayName));
        who.mailReceived.Add("petLoveMessage");
      }
      this.doEmote(20, true);
      this.playContentSound();
      return true;
    }

    public virtual void playContentSound()
    {
    }

    public void hold(Farmer who)
    {
      this.flip = this.Sprite.CurrentAnimation.Last<FarmerSprite.AnimationFrame>().flip;
      this.Sprite.currentFrame = this.Sprite.CurrentAnimation.Last<FarmerSprite.AnimationFrame>().frame;
      this.Sprite.CurrentAnimation = (List<FarmerSprite.AnimationFrame>)null;
      this.Sprite.loop = false;
    }

    public override void behaviorOnFarmerPushing()
    {
      if (this is LivelyDog && (this as LivelyDog).CurrentBehavior == 51)
        return;
      this.pushingTimer += 2;
      if (this.pushingTimer <= 100)
        return;
      Vector2 playerTrajectory = Utility.getAwayFromPlayerTrajectory(this.GetBoundingBox(), Game1.player);
      this.setTrajectory((int)playerTrajectory.X / 2, (int)playerTrajectory.Y / 2);
      this.pushingTimer = 0;
      this.Halt();
      this.facePlayer(Game1.player);
      this.FacingDirection += 2;
      this.FacingDirection %= 4;
      this.faceDirection(this.FacingDirection);
      this.CurrentBehavior = 0;
    }

    public override void update(GameTime time, GameLocation location, long id, bool move)
    {
      /*
      if (startedBehavior == -1)
      {
        base.update(time, location, id, move);
        return;
      }
      */

      if (!isNearFarmer)
      {
        //Halt();
        //Sprite.StopAnimation();
        moveTowardFarmer(Game1.player, location, time);
      }
      else
      {
        if (startedBehavior != CurrentBehavior)
        {
          initiateCurrentBehavior();
        }
      }
      pushingTimer = Math.Max(0, pushingTimer - 1);
    }

    private void moveTowardFarmer(Farmer farmer, GameLocation location, GameTime time)
    {
      if (pathToFarmer == null) return;
      
    if (pathingIndex < pathToFarmer.Count)
      {
        switch (pathToFarmer[pathingIndex])
        {
          case 0:
            this.SetMovingOnlyUp();
            break;
          case 1:
            this.SetMovingOnlyRight();
            break;
          case 2:
            this.SetMovingOnlyDown();
            break;
          case 3:
            this.SetMovingOnlyLeft();
            break;
          default:
            break;
        }

        MoveAlongPath(time, Game1.viewport);
      }
    }

    public void MoveAlongPath(GameTime time, xTile.Dimensions.Rectangle viewport)
    {
      Speed = 4;

      willDestroyObjectsUnderfoot = false;
      TimeSpan elapsedGameTime;
      var step = speed + addedSpeed;
      if (xVelocity != 0f || yVelocity != 0f)
      {
        applyVelocity(currentLocation);
      }
      else if (moveUp)
      {
        if ((Position.Y % 64 != 0) && position.Y - step < (int) Math.Floor(Position.Y / Game1.tileSize) * Game1.tileSize)
        {
          step = (int)(Math.Floor(Position.Y / Game1.tileSize) * Game1.tileSize - Position.Y);
          pathingIndex++;
        }

        position.Y -= step;

        if (!ignoreMovementAnimation)
        {
          Sprite.AnimateUp(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
          faceDirection(0);
        }
      }
      else if (moveRight)
      {
        if ((Position.X % 64 != 0) && position.X + step > (int)Math.Ceiling(Position.X / Game1.tileSize) * Game1.tileSize)
        {
          step = (int)(Math.Ceiling(Position.X / Game1.tileSize) * Game1.tileSize - Position.X);
          pathingIndex++;
        }
        position.X += step;

        if (!ignoreMovementAnimation)
        {
          Sprite.AnimateRight(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
          faceDirection(1);
        }
      }
      else if (moveDown)
      {
        if ((Position.Y % 64 != 0) && position.Y + step > (int)Math.Ceiling(Position.Y / Game1.tileSize) * Game1.tileSize)
        {
          step = (int)(Math.Ceiling(Position.Y / Game1.tileSize) * Game1.tileSize - Position.Y);
          pathingIndex++;
        }
        position.Y += step;
        if (!ignoreMovementAnimation)
        {
          Sprite.AnimateDown(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
          faceDirection(2);
        }
      }
      else if (moveLeft)
      {
        if ((Position.X % 64 != 0) && position.X - step < (int)Math.Floor(Position.X / Game1.tileSize) * Game1.tileSize)
        {
          step = (int)(Math.Floor(Position.X / Game1.tileSize) * Game1.tileSize - Position.X);
          pathingIndex++;
        }
        position.X -= step;

        if (!ignoreMovementAnimation)
        {
          Sprite.AnimateLeft(time, (speed - 2 + addedSpeed) * -25, Utility.isOnScreen(getTileLocationPoint(), 1, currentLocation) ? "Cowboy_Footstep" : "");
          faceDirection(3);
        }
      }
      else
      {
        Sprite.animateOnce(time);
      }
 

      if (blockedInterval >= 3000 && (float)blockedInterval <= 3750f && !Game1.eventUp)
      {
        doEmote((Game1.random.NextDouble() < 0.5) ? 8 : 40, true);
        blockedInterval = 3750;
      }
      else if (blockedInterval >= 5000)
      {
        speed = 4;
        isCharging = true;
        blockedInterval = 0;
      }
    }

    public void UpdatePathToFarmer()
    {
      isNearFarmer = (getTileX() - activeFarmer.getTileX()) * (getTileX() - activeFarmer.getTileX()) + (getTileY() - activeFarmer.getTileY()) * (getTileY() - activeFarmer.getTileY()) < proximity * proximity;
      if (isNearFarmer || currentLocation != activeFarmer.currentLocation) return;
      if (prevFarmerPos == activeFarmer.getTileLocation()) return;
      prevFarmerPos = activeFarmer.getTileLocation();
      pathingIndex = 0;
      Vector2 tile = activeFarmer.getTileLocation();

      // Get nearest tile adjacent to farmer
      if (activeFarmer.getTileX() < getTileX())
      {
        if (!currentLocation.isCollidingPosition(activeFarmer.nextPosition(3), Game1.viewport, false, 0, false, this))
        {
          tile = new Vector2(activeFarmer.nextPosition(3).X/Game1.tileSize, activeFarmer.nextPosition(3).Y / Game1.tileSize);
        }
      }
      else
      {
        if (!currentLocation.isCollidingPosition(activeFarmer.nextPosition(1), Game1.viewport, false, 0, false, this))
        {
          tile = new Vector2(activeFarmer.nextPosition(1).X / Game1.tileSize, activeFarmer.nextPosition(1).Y / Game1.tileSize);
        }
      }

      if (activeFarmer.getTileY() < getTileY())
      {
        if (!currentLocation.isCollidingPosition(activeFarmer.nextPosition(2), Game1.viewport, false, 0, false, this))
        {
          tile = new Vector2(activeFarmer.nextPosition(2).X / Game1.tileSize, activeFarmer.nextPosition(2).Y / Game1.tileSize);
        }
      }
      else
      {
        if (!currentLocation.isCollidingPosition(activeFarmer.nextPosition(0), Game1.viewport, false, 0, false, this))
        {
          tile = new Vector2(activeFarmer.nextPosition(0).X / Game1.tileSize, activeFarmer.nextPosition(0).Y / Game1.tileSize);
        }
      }
      start = getStandingPosition();
      pathToFarmer = ModUtil.GetPath(currentLocation, new Vector2((int)Math.Ceiling(Position.X/Game1.tileSize), (int)Math.Ceiling(Position.Y/Game1.tileSize)), new Vector2((int)Math.Ceiling(activeFarmer.Position.X / Game1.tileSize), (int)Math.Ceiling(activeFarmer.Position.Y / Game1.tileSize)), this);
    }

    protected override void updateSlaveAnimation(GameTime time)
    {
    }

    public virtual void initiateCurrentBehavior()
    {
      this.flip = false;
      bool flip1 = false;
      switch (this.CurrentBehavior)
      {
        case 0:
          this.Halt();
          this.faceDirection(Game1.random.Next(4));
          if (Game1.IsMasterGame)
          {
            this.setMovingInFacingDirection();
            break;
          }
          break;
        case 1:
          this.Sprite.loop = true;
          bool flip2 = Game1.random.NextDouble() < 0.5;
          this.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>()
          {
              new FarmerSprite.AnimationFrame(28, 1000, false, flip2, (AnimatedSprite.endOfAnimationBehavior)null, false),
              new FarmerSprite.AnimationFrame(29, 1000, false, flip2, (AnimatedSprite.endOfAnimationBehavior)null, false)
          });
          break;
        case 2:
          this.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>()
          {
              new FarmerSprite.AnimationFrame(16, 100, false, flip1, (AnimatedSprite.endOfAnimationBehavior)null, false),
              new FarmerSprite.AnimationFrame(17, 100, false, flip1, (AnimatedSprite.endOfAnimationBehavior)null, false),
              new FarmerSprite.AnimationFrame(18, 100, false, flip1, new AnimatedSprite.endOfAnimationBehavior(this.hold), false)
          });
          break;
      }
      this.startedBehavior = this.CurrentBehavior;
    }

    public override Rectangle GetBoundingBox()
    {
      return new Rectangle((int)this.Position.X+16, (int)this.Position.Y+16, 32, 32);
    }

    public override void draw(SpriteBatch b)
    {
      base.draw(b);
      if (!this.IsEmoting)
        return;
      Vector2 localPosition = this.getLocalPosition(Game1.viewport);
      localPosition.X += 32f;
      localPosition.Y -= (float)(96 + (this is Dog ? 16 : 0));
      b.Draw(Game1.emoteSpriteSheet, localPosition, new Rectangle?(new Rectangle(this.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, this.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, (float)((double)this.getStandingY() / 10000.0 + 9.99999974737875E-05));
    }
  }

}
