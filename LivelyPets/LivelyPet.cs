using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

namespace LivelyPets
{
  public class LivelyPet : Pet
  {
    public const int bedTime = 2000;
    public const int maxFriendship = 1000;
    public const int behavior_walking = 0;
    public const int behavior_Sleep = 1;
    public const int behavior_Sit_Down = 2;
    public const int frame_basicSit = 18;
    private readonly NetInt netCurrentBehavior = new NetInt();
    private int startedBehavior = -1;
    private bool wasPetToday;
    public int friendshipTowardFarmer;
    private int pushingTimer;
    private int skipHorizontal;
    private bool skipHorizontalUp;
    private int durationOfRandomMovements = 100;

    private int closenessLevel;
    private int obedienceLevel;

    public int CurrentBehavior
    {
      get
      {
        if (isMoving())
        {
          return 0;
        }
        return netCurrentBehavior;
      }
      set
      {
        netCurrentBehavior.Value = value;
      }
    }

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
      if (startedBehavior != CurrentBehavior)
      {
        initiateCurrentBehavior();
      }
      //moveTowardFarmer(Game1.player, time, location);
      base.update(time, location, id, move);
      pushingTimer = Math.Max(0, pushingTimer - 1);
    }

    private void moveTowardFarmer(Farmer farmer, GameTime time, GameLocation location)
    {
      if (((int)moveTowardPlayerThreshold.Value == -1 || withinPlayerThreshold()) && timeBeforeAIMovementAgain <= 0f && IsMonster && location.map.GetLayer("Back").Tiles[(int)farmer.getTileLocation().X, (int)farmer.getTileLocation().Y] != null && !location.map.GetLayer("Back").Tiles[(int)farmer.getTileLocation().X, (int)farmer.getTileLocation().Y].Properties.ContainsKey("NPCBarrier"))
      {
        if (skipHorizontal <= 0)
        {
          if (lastPosition.Equals(base.Position) && Game1.random.NextDouble() < 0.001)
          {
            switch (base.FacingDirection)
            {
              case 1:
              case 3:
                if (Game1.random.NextDouble() < 0.5)
                {
                  SetMovingOnlyUp();
                }
                else
                {
                  SetMovingOnlyDown();
                }
                break;
              case 0:
              case 2:
                if (Game1.random.NextDouble() < 0.5)
                {
                  SetMovingOnlyRight();
                }
                else
                {
                  SetMovingOnlyLeft();
                }
                break;
            }
            skipHorizontal = 700;
            return;
          }
          bool success = false;
          bool setMoving = false;
          bool scootSuccess = false;
          if (lastPosition.X == base.Position.X)
          {
            checkHorizontalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
            checkVerticalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
          }
          else
          {
            checkVerticalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
            checkHorizontalMovement(ref success, ref setMoving, ref scootSuccess, farmer, location);
          }
          if (!success && !setMoving)
          {
            Halt();
            faceGeneralDirection(farmer.getStandingPosition(), 0, false);
          }
          if (success)
          {
            skipHorizontal = 500;
          }
          if (scootSuccess)
          {
            return;
          }
        }
        else
        {
          skipHorizontal -= time.ElapsedGameTime.Milliseconds;
        }
      }

      MovePosition(time, Game1.viewport, location);
      if (base.Position.Equals(lastPosition) && base.IsWalkingTowardPlayer && withinPlayerThreshold())
      {
        Halt();
        faceGeneralDirection(farmer.getStandingPosition(), 0, false);
      }
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
