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

namespace LivelyPets
{
  public class LivelyPet : Pet
  {
    public IMonitor Monitor;
    private readonly NetInt netCurrentBehavior = new NetInt();
    private int startedBehavior = -1;
    private bool wasPetToday;
    private int pushingTimer;
    public bool isByFarmer;
    private int skipHorizontal;
    private bool skipHorizontalUp;
    private int durationOfRandomMovements = 0;


    private int closenessLevel;
    private int obedienceLevel;

    protected override void initNetFields()
    {
      base.initNetFields();
      base.NetFields.AddFields(netCurrentBehavior);
    }

    public override void behaviorOnFarmerLocationEntry(GameLocation location, Farmer who)
    {
      if ((location is Farm || (location is FarmHouse && CurrentBehavior != 1)) && Game1.timeOfDay >= 2000)
      {
        if (CurrentBehavior != 1 || base.currentLocation is Farm)
        {
          warpToFarmHouse(who);
        }
      }
      else if (Game1.timeOfDay < 2000 && Game1.random.NextDouble() < 0.5)
      {
        CurrentBehavior = 1;
      }
    }

    public override bool canTalk()
    {
      return false;
    }

    public override void reloadSprite()
    {
      base.DefaultPosition = new Vector2(54f, 8f) * 64f;
      base.HideShadow = true;
      base.Breather = false;
      setAtFarmPosition();
    }

    public void warpToFarmHouse(Farmer who)
    {
      FarmHouse farmHouse = Utility.getHomeOfFarmer(who);
      Vector2 sleepTile2 = Vector2.Zero;
      int tries = 0;
      sleepTile2 = new Vector2((float)Game1.random.Next(2, farmHouse.map.Layers[0].LayerWidth - 3), (float)Game1.random.Next(3, farmHouse.map.Layers[0].LayerHeight - 5));
      for (; tries < 50; tries++)
      {
        if (farmHouse.isTileLocationTotallyClearAndPlaceable(sleepTile2) && farmHouse.isTileLocationTotallyClearAndPlaceable(sleepTile2 + new Vector2(1f, 0f)) && !farmHouse.isTileOnWall((int)sleepTile2.X, (int)sleepTile2.Y))
        {
          break;
        }
        sleepTile2 = new Vector2((float)Game1.random.Next(2, farmHouse.map.Layers[0].LayerWidth - 3), (float)Game1.random.Next(3, farmHouse.map.Layers[0].LayerHeight - 4));
      }
      if (tries < 50)
      {
        Game1.warpCharacter(this, "FarmHouse", sleepTile2);
        CurrentBehavior = 1;
        initiateCurrentBehavior();
      }
    }

    public override void dayUpdate(int dayOfMonth)
    {
      base.DefaultPosition = new Vector2(54f, 8f) * 64f;
      Sprite.loop = false;
      base.Breather = false;
      if (Game1.isRaining)
      {
        CurrentBehavior = 2;
        if (base.currentLocation is Farm)
        {
          warpToFarmHouse(Game1.player);
        }
      }
      else if (base.currentLocation is FarmHouse)
      {
        setAtFarmPosition();
      }
      if (base.currentLocation is Farm)
      {
        if (base.currentLocation.getTileIndexAt(54, 7, "Buildings") == 1939)
        {
          friendshipTowardFarmer = Math.Min(1000, friendshipTowardFarmer + 6);
        }
        base.currentLocation.setMapTileIndex(54, 7, 1938, "Buildings", 0);
        setTilePosition(54, 8);
        position.X -= 64f;
      }
      Halt();
      CurrentBehavior = 1;
      wasPetToday = false;
    }

    public void setAtFarmPosition()
    {
      bool isOnFarm = base.currentLocation is Farm;
      if (!Game1.isRaining)
      {
        faceDirection(2);
        Game1.warpCharacter(this, "Farm", new Vector2(54f, 8f));
        position.X -= 64f;
      }
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
      if (!wasPetToday)
      {
        wasPetToday = true;
        friendshipTowardFarmer = Math.Min(1000, friendshipTowardFarmer + 12);
        if (friendshipTowardFarmer >= 1000 && who != null && !who.mailReceived.Contains("petLoveMessage"))
        {
          Game1.showGlobalMessage(Game1.content.LoadString("Strings\\Characters:PetLovesYou", base.displayName));
          who.mailReceived.Add("petLoveMessage");
        }
        doEmote(20, true);
        playContentSound();
        return true;
      }
      return false;
    }

    public virtual void playContentSound()
    {
    }

    public void hold(Farmer who)
    {
      flip = Sprite.CurrentAnimation.Last().flip;
      Sprite.currentFrame = Sprite.CurrentAnimation.Last().frame;
      Sprite.CurrentAnimation = null;
      Sprite.loop = false;
    }

    public override void behaviorOnFarmerPushing()
    {
      if (!(this is LivelyDog) || (this as LivelyDog).CurrentBehavior != 51)
      {
        pushingTimer += 2;
        if (pushingTimer > 100)
        {
          Vector2 trajectory = Utility.getAwayFromPlayerTrajectory(GetBoundingBox(), Game1.player);
          setTrajectory((int)trajectory.X / 2, (int)trajectory.Y / 2);
          pushingTimer = 0;
          Halt();
          facePlayer(Game1.player);
          base.FacingDirection += 2;
          base.FacingDirection %= 4;
          faceDirection(base.FacingDirection);
          CurrentBehavior = 0;
        }
      }
    }

    public override void update(GameTime time, GameLocation location, long id, bool move)
    {
      /*
      if (startedBehavior != CurrentBehavior)
      {
        initiateCurrentBehavior();
      }
      */
      moveTowardFarmer(Game1.player, location, time);

      pushingTimer = Math.Max(0, pushingTimer - 1);
    }


    public override void updateMovement(GameLocation location, GameTime time)
    {
      moveTowardFarmer(Game1.player, location, time );
    }


    private void moveTowardFarmer(Farmer farmer, GameLocation location, GameTime time)
    {
      if (this.IsWalkingTowardPlayer)
      {
        if (((int)((NetFieldBase<int, NetInt>)this.moveTowardPlayerThreshold) == -1 || this.withinPlayerThreshold()) && ((double)this.timeBeforeAIMovementAgain <= 0.0) && location.map.GetLayer("Back").Tiles[(int)farmer.getTileLocation().X, (int)farmer.getTileLocation().Y] != null && !location.map.GetLayer("Back").Tiles[(int)farmer.getTileLocation().X, (int)farmer.getTileLocation().Y].Properties.ContainsKey("NPCBarrier"))
        {
          Monitor.Log($"{time.ElapsedGameTime} - Move Update");
          if (this.skipHorizontal <= 0)
          {
            if (this.lastPosition.Equals(this.Position) && Game1.random.NextDouble() < 0.001)
            {
              switch (this.FacingDirection)
              {
                case 0:
                case 2:
                  if (Game1.random.NextDouble() < 0.5)
                  {
                    this.SetMovingOnlyRight();
                    break;
                  }
                  this.SetMovingOnlyLeft();
                  break;
                case 1:
                case 3:
                  if (Game1.random.NextDouble() < 0.5)
                  {
                    this.SetMovingOnlyUp();
                    break;
                  }
                  this.SetMovingOnlyDown();
                  break;
              }
              this.skipHorizontal = 700;
              return;
            }
            bool success = false;
            bool setMoving = false;
            bool scootSuccess = false;
            if ((double)this.lastPosition.X == (double)this.Position.X)
            {
              this.checkHorizontalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
              this.checkVerticalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
            }
            else
            {
              this.checkVerticalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
              this.checkHorizontalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
            }
            if (!success && !setMoving)
            {
              this.Halt();
              this.faceGeneralDirection(farmer.getStandingPosition(), 0, false);
            }
            if (success)
              this.skipHorizontal = 500;
            if (scootSuccess)
              return;
          }
          else
            this.skipHorizontal -= time.ElapsedGameTime.Milliseconds;
        }
      }
      else
        this.defaultMovementBehavior(time);
      this.MovePosition(time, Game1.viewport, location);
      if (!this.Position.Equals(this.lastPosition) || !this.IsWalkingTowardPlayer || !this.withinPlayerThreshold())
        return;

      this.Halt();
      this.faceGeneralDirection(farmer.getStandingPosition(), 0, false);
    }


        public virtual void defaultMovementBehavior(GameTime time)
    {
      switch (Game1.random.Next(6))
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
          this.Halt();
          break;
      }
    }

    public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
    {
      this.lastPosition = this.Position;
      if ((double)this.xVelocity != 0.0 || (double)this.yVelocity != 0.0)
      {
        if (double.IsNaN((double)this.xVelocity) || double.IsNaN((double)this.yVelocity))
        {
          this.xVelocity = 0.0f;
          this.yVelocity = 0.0f;
        }
        Microsoft.Xna.Framework.Rectangle boundingBox = this.GetBoundingBox();
        boundingBox.X += (int)this.xVelocity;
        boundingBox.Y -= (int)this.yVelocity;
        if (!currentLocation.isCollidingPosition(boundingBox, viewport, false, 0, false, (Character)this))
        {
          this.position.X += this.xVelocity;
          this.position.Y -= this.yVelocity;
          this.xVelocity -= this.xVelocity;
          this.yVelocity -= this.yVelocity;
          if ((double)Math.Abs(this.xVelocity) <= 0.0500000007450581)
            this.xVelocity = 0.0f;
          if ((double)Math.Abs(this.yVelocity) <= 0.0500000007450581)
            this.yVelocity = 0.0f;
        }
      }
      if (this.moveUp)
      {
        if (!currentLocation.isCollidingPosition(this.nextPosition(0), viewport, false, 0, false, (Character)this) || this.isCharging)
        {
          this.position.Y -= (float)(this.speed + this.addedSpeed);
          if (!this.ignoreMovementAnimations)
            this.Sprite.AnimateUp(time, 0, "");
          this.FacingDirection = 0;
          this.faceDirection(0);
        }
        else
        {
          Microsoft.Xna.Framework.Rectangle position = this.nextPosition(0);
          position.Width /= 4;
          bool flag1 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          position.X += position.Width * 3;
          bool flag2 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          if (flag1 && !flag2 && !currentLocation.isCollidingPosition(this.nextPosition(1), viewport, false, 0, false, (Character)this))
            this.position.X += (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          else if (flag2 && !flag1 && !currentLocation.isCollidingPosition(this.nextPosition(3), viewport, false, 0, false, (Character)this))
            this.position.X -= (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          if (!currentLocation.isTilePassable(this.nextPosition(0), viewport) || !this.willDestroyObjectsUnderfoot)
            this.Halt();
          else if (this.willDestroyObjectsUnderfoot)
          {
            Vector2 vector2 = new Vector2((float)(this.getStandingX() / 64), (float)(this.getStandingY() / 64 - 1));
            if (currentLocation.characterDestroyObjectWithinRectangle(this.nextPosition(0), true))
            {
              currentLocation.playSound("stoneCrack");
              this.position.Y -= (float)(this.speed + this.addedSpeed);
            }
            else
              this.blockedInterval += time.ElapsedGameTime.Milliseconds;
          }
        }
      }
      else if (this.moveRight)
      {
        if (!currentLocation.isCollidingPosition(this.nextPosition(1), viewport, false, 0, false, (Character)this) || this.isCharging)
        {
          this.position.X += (float)(this.speed + this.addedSpeed);
          if (!this.ignoreMovementAnimations)
            this.Sprite.AnimateRight(time, 0, "");
          this.FacingDirection = 1;
          this.faceDirection(1);
        }
        else
        {
          Microsoft.Xna.Framework.Rectangle position = this.nextPosition(1);
          position.Height /= 4;
          bool flag1 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          position.Y += position.Height * 3;
          bool flag2 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          if (flag1 && !flag2 && !currentLocation.isCollidingPosition(this.nextPosition(2), viewport, false, 0, false, (Character)this))
            this.position.Y += (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          else if (flag2 && !flag1 && !currentLocation.isCollidingPosition(this.nextPosition(0), viewport, false, 0, false, (Character)this))
            this.position.Y -= (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          if (!currentLocation.isTilePassable(this.nextPosition(1), viewport) || !this.willDestroyObjectsUnderfoot)
            this.Halt();
          else if (this.willDestroyObjectsUnderfoot)
          {
            Vector2 vector2 = new Vector2((float)(this.getStandingX() / 64 + 1), (float)(this.getStandingY() / 64));
            if (currentLocation.characterDestroyObjectWithinRectangle(this.nextPosition(1), true))
            {
              currentLocation.playSound("stoneCrack");
              this.position.X += (float)(this.speed + this.addedSpeed);
            }
            else
              this.blockedInterval += time.ElapsedGameTime.Milliseconds;
          }
        }
      }
      else if (this.moveDown)
      {
        if (!currentLocation.isCollidingPosition(this.nextPosition(2), viewport, false, 0, false, (Character)this) || this.isCharging)
        {
          this.position.Y += (float)(this.speed + this.addedSpeed);
          if (!this.ignoreMovementAnimations)
            this.Sprite.AnimateDown(time, 0, "");
          this.FacingDirection = 2;
          this.faceDirection(2);
        }
        else
        {
          Microsoft.Xna.Framework.Rectangle position = this.nextPosition(2);
          position.Width /= 4;
          bool flag1 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          position.X += position.Width * 3;
          bool flag2 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          if (flag1 && !flag2 && !currentLocation.isCollidingPosition(this.nextPosition(1), viewport, false, 0, false, (Character)this))
            this.position.X += (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          else if (flag2 && !flag1 && !currentLocation.isCollidingPosition(this.nextPosition(3), viewport, false, 0, false, (Character)this))
            this.position.X -= (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          if (!currentLocation.isTilePassable(this.nextPosition(2), viewport) || !this.willDestroyObjectsUnderfoot)
            this.Halt();
          else if (this.willDestroyObjectsUnderfoot)
          {
            Vector2 vector2 = new Vector2((float)(this.getStandingX() / 64), (float)(this.getStandingY() / 64 + 1));
            if (currentLocation.characterDestroyObjectWithinRectangle(this.nextPosition(2), true))
            {
              currentLocation.playSound("stoneCrack");
              this.position.Y += (float)(this.speed + this.addedSpeed);
            }
            else
              this.blockedInterval += time.ElapsedGameTime.Milliseconds;
          }
        }
      }
      else if (this.moveLeft)
      {
        if (!currentLocation.isCollidingPosition(this.nextPosition(3), viewport, false, 0, false, (Character)this) || this.isCharging)
        {
          this.position.X -= (float)(this.speed + this.addedSpeed);
          this.FacingDirection = 3;
          if (!this.ignoreMovementAnimations)
            this.Sprite.AnimateLeft(time, 0, "");
          this.faceDirection(3);
        }
        else
        {
          Microsoft.Xna.Framework.Rectangle position = this.nextPosition(3);
          position.Height /= 4;
          bool flag1 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          position.Y += position.Height * 3;
          bool flag2 = currentLocation.isCollidingPosition(position, viewport, false, 0, false, (Character)this);
          if (flag1 && !flag2 && !currentLocation.isCollidingPosition(this.nextPosition(2), viewport, false, 0, false, (Character)this))
            this.position.Y += (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          else if (flag2 && !flag1 && !currentLocation.isCollidingPosition(this.nextPosition(0), viewport, false, 0, false, (Character)this))
            this.position.Y -= (float)this.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
          if (!currentLocation.isTilePassable(this.nextPosition(3), viewport) || !this.willDestroyObjectsUnderfoot)
            this.Halt();
          else if (this.willDestroyObjectsUnderfoot)
          {
            Vector2 vector2 = new Vector2((float)(this.getStandingX() / 64 - 1), (float)(this.getStandingY() / 64));
            if (currentLocation.characterDestroyObjectWithinRectangle(this.nextPosition(3), true))
            {
              currentLocation.playSound("stoneCrack");
              this.position.X -= (float)(this.speed + this.addedSpeed);
            }
            else
              this.blockedInterval += time.ElapsedGameTime.Milliseconds;
          }
        }
      }
      else if (!this.ignoreMovementAnimations)
      {
        if (this.moveUp)
          this.Sprite.AnimateUp(time, 0, "");
        else if (this.moveRight)
          this.Sprite.AnimateRight(time, 0, "");
        else if (this.moveDown)
          this.Sprite.AnimateDown(time, 0, "");
        else if (this.moveLeft)
          this.Sprite.AnimateLeft(time, 0, "");
      }
      if ((this.blockedInterval < 3000 || (double)this.blockedInterval > 3750.0) && this.blockedInterval >= 5000)
      {
        this.speed = 4;
        this.isCharging = true;
        this.blockedInterval = 0;
      }
      if (0 <= 0 || Game1.random.NextDouble() >= 0.000333333333333333)
        return;
    }


    private bool doHorizontalMovementTowardFarmer(Farmer farmer, GameLocation location)
    {
      bool wasAbleToMoveHorizontally = false;
      if (base.Position.X > farmer.Position.X + 8f || (skipHorizontal > 0 && farmer.getStandingX() < getStandingX() - 8))
      {
        SetMovingOnlyLeft();
        if (!location.isCollidingPosition(nextPosition(3), Game1.viewport, false, 0, false, this))
        {
          MovePosition(Game1.currentGameTime, Game1.viewport, location);
          wasAbleToMoveHorizontally = true;
        }
        else
        {
          faceDirection(3);
          if ((int)durationOfRandomMovements > 0 && Game1.random.NextDouble() < 0)
          {
            if (Game1.random.NextDouble() < 0.5)
            {
              tryToMoveInDirection(2, false, 0, false);
            }
            else
            {
              tryToMoveInDirection(0, false, 0, false);
            }
            timeBeforeAIMovementAgain = (float)(int)durationOfRandomMovements;
          }
        }
      }
      else if (base.Position.X < farmer.Position.X - 8f)
      {
        SetMovingOnlyRight();
        if (!location.isCollidingPosition(nextPosition(1), Game1.viewport, false, 0, false, this))
        {
          MovePosition(Game1.currentGameTime, Game1.viewport, location);
          wasAbleToMoveHorizontally = true;
        }
        else
        {
          faceDirection(1);
          if ((int)durationOfRandomMovements > 0 && Game1.random.NextDouble() < 0)
          {
            if (Game1.random.NextDouble() < 0.5)
            {
              tryToMoveInDirection(2, false, 0, false);
            }
            else
            {
              tryToMoveInDirection(0, false, 0, false);
            }
            timeBeforeAIMovementAgain = (float)(int)durationOfRandomMovements;
          }
        }
      }
      else
      {
        faceGeneralDirection(farmer.getStandingPosition(), 0, false);
        setMovingInFacingDirection();
        skipHorizontal = 500;
      }
      return wasAbleToMoveHorizontally;
    }

    private void checkHorizontalMovement(ref bool success, ref bool setMoving, ref bool scootSuccess, Farmer who, GameLocation location)
    {
      Vector2 position;
      if (who.Position.X > base.Position.X + 16f)
      {
        SetMovingOnlyRight();
        setMoving = true;
        if (!location.isCollidingPosition(nextPosition(1), Game1.viewport, false, 0, false, this))
        {
          success = true;
        }
        else
        {
          MovePosition(Game1.currentGameTime, Game1.viewport, location);
          position = base.Position;
          if (!position.Equals(lastPosition))
          {
            scootSuccess = true;
          }
        }
      }
      if (!success && who.Position.X < base.Position.X - 16f)
      {
        SetMovingOnlyLeft();
        setMoving = true;
        if (!location.isCollidingPosition(nextPosition(3), Game1.viewport, false, 0, false, this))
        {
          success = true;
        }
        else
        {
          MovePosition(Game1.currentGameTime, Game1.viewport, location);
          position = base.Position;
          if (!position.Equals(lastPosition))
          {
            scootSuccess = true;
          }
        }
      }
    }

    private void checkVerticalMovement(ref bool success, ref bool setMoving, ref bool scootSuccess, Farmer who,
      GameLocation location)
    {
      Vector2 position;
      if (!success && who.Position.Y < base.Position.Y - 16f)
      {
        SetMovingOnlyUp();
        setMoving = true;
        if (!location.isCollidingPosition(nextPosition(0), Game1.viewport, false, 0, false, this))
        {
          success = true;
        }
        else
        {
          MovePosition(Game1.currentGameTime, Game1.viewport, location);
          position = base.Position;
          if (!position.Equals(lastPosition))
          {
            scootSuccess = true;
          }
        }
      }

      if (!success && who.Position.Y > base.Position.Y + 16f)
      {
        SetMovingOnlyDown();
        setMoving = true;
        if (!location.isCollidingPosition(nextPosition(2), Game1.viewport, false, 0, false, this))
        {
          success = true;
        }
        else
        {
          MovePosition(Game1.currentGameTime, Game1.viewport, location);
          position = base.Position;
          if (!position.Equals(lastPosition))
          {
            scootSuccess = true;
          }
        }
      }
    }

    public virtual void initiateCurrentBehavior()
    {
      flip = false;
      bool localFlip2 = false;

      switch (CurrentBehavior)
      {
        case 1:
          Sprite.loop = true;
          localFlip2 = (Game1.random.NextDouble() < 0.5);
          Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
      {
        new FarmerSprite.AnimationFrame(28, 1000, false, localFlip2, null, false),
        new FarmerSprite.AnimationFrame(29, 1000, false, localFlip2, null, false)
      });
          break;
        case 2:
          Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
      {
        new FarmerSprite.AnimationFrame(16, 100, false, localFlip2, null, false),
        new FarmerSprite.AnimationFrame(17, 100, false, localFlip2, null, false),
        new FarmerSprite.AnimationFrame(18, 100, false, localFlip2, hold, false)
      });
          break;
        case 0:
          Halt();
          faceDirection(Game1.random.Next(4));
          if (Game1.IsMasterGame)
          {
            setMovingInFacingDirection();
          }
          break;
      }
      startedBehavior = CurrentBehavior;
    }

    public override Rectangle GetBoundingBox()
    {
      return new Rectangle((int)base.Position.X + 16, (int)base.Position.Y + 16, Sprite.SpriteWidth * 4 * 3 / 4, 32);
    }

    public override void draw(SpriteBatch b)
    {
      base.draw(b);
      if (base.IsEmoting)
      {
        Vector2 emotePosition = getLocalPosition(Game1.viewport);
        emotePosition.X += 32f;
        emotePosition.Y -= (float)(96 + ((this is Dog) ? 16 : 0));
        b.Draw(Game1.emoteSpriteSheet, emotePosition, new Rectangle(base.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, base.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)getStandingY() / 10000f + 0.0001f);
      }
    }
  }

}
