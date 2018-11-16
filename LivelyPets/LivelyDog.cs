using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;

namespace LivelyPets
{
  class LivelyDog : LivelyPet
  {
    public const int behavior_sit_right = 50;
    public const int behavior_sprint = 51;
    private int sprintTimer;
    private bool wagging;

    public LivelyDog(Dog pet)
    {
      var petType = pet.GetType();
      PropertyInfo[] properties = petType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
      FieldInfo[] fields = petType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

      foreach (PropertyInfo property in properties)
      {
        try
        {
          property.SetValue(pet, property.GetValue(pet, null), null);
        }
        catch (ArgumentException) { } // For Get-only-properties
      }
      foreach (FieldInfo field in fields)
      {
        field.SetValue(this, field.GetValue(pet));
      }
    }

    public LivelyDog(int xTile, int yTile)
    {
      Sprite = new AnimatedSprite("Animals\\dog", 0, 32, 32);
      base.Position = new Vector2((float)xTile, (float)yTile) * 64f;
      base.Breather = false;
      willDestroyObjectsUnderfoot = false;
      base.currentLocation = Game1.currentLocation;
      base.HideShadow = true;
    }

    public override void dayUpdate(int dayOfMonth)
    {
      base.dayUpdate(dayOfMonth);
      sprintTimer = 0;
    }

    public override void update(GameTime time, GameLocation location)
    {
      base.update(time, location);
      if (base.currentLocation == null)
      {
        base.currentLocation = location;
      }
      if (!Game1.eventUp && !Game1.IsClient)
      {
        if (sprintTimer > 0)
        {
          Sprite.loop = true;
          sprintTimer -= time.ElapsedGameTime.Milliseconds;
          base.speed = 6;
          tryToMoveInDirection(base.FacingDirection, false, -1, false);
          if (sprintTimer <= 0)
          {
            Sprite.CurrentAnimation = null;
            Halt();
            faceDirection(base.FacingDirection);
            base.speed = 2;
            base.CurrentBehavior = 0;
          }
        }
        else
        {
          if (Game1.timeOfDay > 2000 && Sprite.CurrentAnimation == null && xVelocity == 0f && yVelocity == 0f)
          {
            base.CurrentBehavior = 1;
          }
          switch (base.CurrentBehavior)
          {
            case 1:
              if (Game1.timeOfDay < 2000 && Game1.random.NextDouble() < 0.001)
              {
                base.CurrentBehavior = 0;
              }
              else if (Game1.random.NextDouble() < 0.002)
              {
                doEmote(24, true);
              }
              return;
            case 2:
              if (Sprite.currentFrame != 18 && Sprite.CurrentAnimation == null)
              {
                base.CurrentBehavior = 2;
              }
              else if (Sprite.currentFrame == 18 && Game1.random.NextDouble() < 0.01)
              {
                switch (Game1.random.Next(4))
                {
                  case 0:
                    base.CurrentBehavior = 0;
                    Halt();
                    faceDirection(2);
                    Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(17, 200),
                new FarmerSprite.AnimationFrame(16, 200),
                new FarmerSprite.AnimationFrame(0, 200)
              });
                    Sprite.loop = false;
                    break;
                  case 1:
                    {
                      List<FarmerSprite.AnimationFrame> pant = new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(18, 200, false, false, pantSound, false),
                new FarmerSprite.AnimationFrame(19, 200)
              };
                      int pants = Game1.random.Next(7, 20);
                      for (int i = 0; i < pants; i++)
                      {
                        pant.Add(new FarmerSprite.AnimationFrame(18, 200, false, false, pantSound, false));
                        pant.Add(new FarmerSprite.AnimationFrame(19, 200));
                      }
                      Sprite.setCurrentAnimation(pant);
                      break;
                    }
                  case 2:
                  case 3:
                    Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(27, (Game1.random.NextDouble() < 0.3) ? 500 : Game1.random.Next(2000, 15000)),
                new FarmerSprite.AnimationFrame(18, 1, false, false, base.hold, false)
              });
                    Sprite.loop = false;
                    break;
                }
              }
              break;
            case 50:
              if (withinPlayerThreshold(2))
              {
                if (!wagging)
                {
                  wag(base.FacingDirection == 3);
                }
              }
              else if (Sprite.currentFrame != 23 && Sprite.CurrentAnimation == null)
              {
                Sprite.currentFrame = 23;
              }
              else if (Sprite.currentFrame == 23 && Game1.random.NextDouble() < 0.01)
              {
                bool localFlip = base.FacingDirection == 3;
                switch (Game1.random.Next(7))
                {
                  case 0:
                    base.CurrentBehavior = 0;
                    Halt();
                    faceDirection((!localFlip) ? 1 : 3);
                    Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(23, 100, false, localFlip, null, false),
                new FarmerSprite.AnimationFrame(22, 100, false, localFlip, null, false),
                new FarmerSprite.AnimationFrame(21, 100, false, localFlip, null, false),
                new FarmerSprite.AnimationFrame(20, 100, false, localFlip, base.hold, false)
              });
                    Sprite.loop = false;
                    break;
                  case 1:
                    if (Utility.isOnScreen(getTileLocationPoint(), 640, base.currentLocation))
                    {
                      Game1.playSound("dog_bark");
                      shake(500);
                    }
                    Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(26, 500, false, localFlip, null, false),
                new FarmerSprite.AnimationFrame(23, 1, false, localFlip, base.hold, false)
              });
                    break;
                  case 2:
                    wag(localFlip);
                    break;
                  case 3:
                  case 4:
                    Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(23, Game1.random.Next(2000, 6000), false, localFlip, null, false),
                new FarmerSprite.AnimationFrame(23, 1, false, localFlip, base.hold, false)
              });
                    break;
                  default:
                    {
                      Sprite.loop = false;
                      List<FarmerSprite.AnimationFrame> panting = new List<FarmerSprite.AnimationFrame>
              {
                new FarmerSprite.AnimationFrame(24, 200, false, localFlip, pantSound, false),
                new FarmerSprite.AnimationFrame(25, 200, false, localFlip, null, false)
              };
                      int pantings = Game1.random.Next(5, 15);
                      for (int j = 0; j < pantings; j++)
                      {
                        panting.Add(new FarmerSprite.AnimationFrame(24, 200, false, localFlip, pantSound, false));
                        panting.Add(new FarmerSprite.AnimationFrame(25, 200, false, localFlip, null, false));
                      }
                      Sprite.setCurrentAnimation(panting);
                      break;
                    }
                }
              }
              break;
            case 0:
              if (Sprite.CurrentAnimation == null && Game1.random.NextDouble() < 0.01)
              {
                switch (Game1.random.Next(7 + ((base.currentLocation is Farm) ? 1 : 0)))
                {
                  case 0:
                  case 1:
                  case 2:
                  case 3:
                    base.CurrentBehavior = 0;
                    break;
                  case 4:
                  case 5:
                    switch (base.FacingDirection)
                    {
                      case 2:
                        Halt();
                        faceDirection(2);
                        Sprite.loop = false;
                        base.CurrentBehavior = 2;
                        break;
                      case 0:
                      case 1:
                      case 3:
                        Halt();
                        if (base.FacingDirection == 0)
                        {
                          base.FacingDirection = ((!(Game1.random.NextDouble() < 0.5)) ? 1 : 3);
                        }
                        faceDirection(base.FacingDirection);
                        Sprite.loop = false;
                        base.CurrentBehavior = 50;
                        break;
                    }
                    break;
                  case 6:
                  case 7:
                    base.CurrentBehavior = 51;
                    break;
                }
              }
              break;
          }
          if (Sprite.CurrentAnimation != null)
          {
            Sprite.loop = false;
          }
          else
          {
            wagging = false;
          }
          /*
          if (Game1.IsMasterGame && Sprite.CurrentAnimation == null)
          {
            MovePosition(time, Game1.viewport, location);
          }
          */
        }
      }
    }

    protected override void updateSlaveAnimation(GameTime time)
    {
      switch (base.CurrentBehavior)
      {
        case 1:
          if (Game1.random.NextDouble() < 0.002)
          {
            doEmote(24, true);
          }
          return;
        case 2:
          if (Sprite.currentFrame == 18 && Game1.random.NextDouble() < 0.01)
          {
            switch (Game1.random.Next(4))
            {
              case 1:
                {
                  List<FarmerSprite.AnimationFrame> pant = new List<FarmerSprite.AnimationFrame>
          {
            new FarmerSprite.AnimationFrame(18, 200, false, false, pantSound, false),
            new FarmerSprite.AnimationFrame(19, 200)
          };
                  int pants = Game1.random.Next(7, 20);
                  for (int i = 0; i < pants; i++)
                  {
                    pant.Add(new FarmerSprite.AnimationFrame(18, 200, false, false, pantSound, false));
                    pant.Add(new FarmerSprite.AnimationFrame(19, 200));
                  }
                  Sprite.setCurrentAnimation(pant);
                  break;
                }
              case 2:
              case 3:
                Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
          {
            new FarmerSprite.AnimationFrame(27, (Game1.random.NextDouble() < 0.3) ? 500 : Game1.random.Next(2000, 15000)),
            new FarmerSprite.AnimationFrame(18, 1, false, false, base.hold, false)
          });
                Sprite.loop = false;
                break;
            }
          }
          break;
        case 50:
          if (withinPlayerThreshold(2))
          {
            if (!wagging)
            {
              wag(base.FacingDirection == 3);
            }
          }
          else if (Sprite.currentFrame != 23 && Sprite.CurrentAnimation == null)
          {
            Sprite.currentFrame = 23;
          }
          else if (Sprite.currentFrame == 23 && Game1.random.NextDouble() < 0.01)
          {
            bool localFlip = base.FacingDirection == 3;
            switch (Game1.random.Next(7))
            {
              case 1:
                if (Utility.isOnScreen(getTileLocationPoint(), 640, base.currentLocation))
                {
                  Game1.playSound("dog_bark");
                  shake(500);
                }
                Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
          {
            new FarmerSprite.AnimationFrame(26, 500, false, localFlip, null, false),
            new FarmerSprite.AnimationFrame(23, 1, false, localFlip, base.hold, false)
          });
                break;
              case 2:
                wag(localFlip);
                break;
              case 3:
              case 4:
                Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
          {
            new FarmerSprite.AnimationFrame(23, Game1.random.Next(2000, 6000), false, localFlip, null, false),
            new FarmerSprite.AnimationFrame(23, 1, false, localFlip, base.hold, false)
          });
                break;
              default:
                {
                  Sprite.loop = false;
                  List<FarmerSprite.AnimationFrame> panting = new List<FarmerSprite.AnimationFrame>
          {
            new FarmerSprite.AnimationFrame(24, 200, false, localFlip, pantSound, false),
            new FarmerSprite.AnimationFrame(25, 200, false, localFlip, null, false)
          };
                  int pantings = Game1.random.Next(5, 15);
                  for (int j = 0; j < pantings; j++)
                  {
                    panting.Add(new FarmerSprite.AnimationFrame(24, 200, false, localFlip, pantSound, false));
                    panting.Add(new FarmerSprite.AnimationFrame(25, 200, false, localFlip, null, false));
                  }
                  Sprite.setCurrentAnimation(panting);
                  break;
                }
              case 0:
                break;
            }
          }
          break;
        case 0:
          faceDirection(base.FacingDirection);
          if (isMoving())
          {
            animateInFacingDirection(time);
          }
          else
          {
            Sprite.StopAnimation();
          }
          break;
      }
      if (Sprite.CurrentAnimation != null)
      {
        Sprite.loop = false;
      }
      else
      {
        wagging = false;
      }
    }

    public void wag(bool localFlip)
    {
      int delay = withinPlayerThreshold(2) ? 120 : 200;
      wagging = true;
      Sprite.loop = false;
      List<FarmerSprite.AnimationFrame> wag = new List<FarmerSprite.AnimationFrame>
    {
      new FarmerSprite.AnimationFrame(31, delay, false, localFlip, null, false),
      new FarmerSprite.AnimationFrame(23, delay, false, localFlip, hitGround, false)
    };
      int wags = Game1.random.Next(2, 6);
      for (int i = 0; i < wags; i++)
      {
        wag.Add(new FarmerSprite.AnimationFrame(31, delay, false, localFlip, null, false));
        wag.Add(new FarmerSprite.AnimationFrame(23, delay, false, localFlip, hitGround, false));
      }
      wag.Add(new FarmerSprite.AnimationFrame(23, 2, false, localFlip, doneWagging, false));
      Sprite.setCurrentAnimation(wag);
    }

    public void doneWagging(Farmer who)
    {
      wagging = false;
    }

    public override void initiateCurrentBehavior()
    {
      sprintTimer = 0;
      base.initiateCurrentBehavior();
      bool localflip2 = base.FacingDirection == 3;
      switch (base.CurrentBehavior)
      {
        case 50:
          Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
      {
        new FarmerSprite.AnimationFrame(20, 100, false, localflip2, null, false),
        new FarmerSprite.AnimationFrame(21, 100, false, localflip2, null, false),
        new FarmerSprite.AnimationFrame(22, 100, false, localflip2, null, false),
        new FarmerSprite.AnimationFrame(23, 100, false, localflip2, base.hold, false)
      });
          break;
        case 51:
          faceDirection((!(Game1.random.NextDouble() < 0.5)) ? 1 : 3);
          localflip2 = (base.FacingDirection == 3);
          sprintTimer = Game1.random.Next(1000, 3500);
          if (Utility.isOnScreen(getTileLocationPoint(), 64, base.currentLocation))
          {
            Game1.playSound("dog_bark");
          }
          Sprite.loop = true;
          Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
      {
        new FarmerSprite.AnimationFrame(32, 100, false, localflip2, null, false),
        new FarmerSprite.AnimationFrame(33, 100, false, localflip2, null, false),
        new FarmerSprite.AnimationFrame(34, 100, false, localflip2, hitGround, false),
        new FarmerSprite.AnimationFrame(33, 100, false, localflip2, null, false)
      });
          break;
      }
    }

    public void hitGround(Farmer who)
    {
      if (Utility.isOnScreen(getTileLocationPoint(), 128, base.currentLocation))
      {
        base.currentLocation.playTerrainSound(getTileLocation(), this, false);
      }
    }

    public void pantSound(Farmer who)
    {
      if (withinPlayerThreshold(5))
      {
        base.currentLocation.localSound("dog_pant");
      }
    }

    public void thumpSound(Farmer who)
    {
      if (withinPlayerThreshold(4))
      {
        base.currentLocation.localSound("thudStep");
      }
      
    }

    public override void playContentSound()
    {
      if (Utility.isOnScreen(getTileLocationPoint(), 128, base.currentLocation))
      {
        Game1.playSound("dog_pant");
        DelayedAction.playSoundAfterDelay("dog_pant", 400, null);
      }
    }
  }

}