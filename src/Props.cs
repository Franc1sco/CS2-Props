using System.Drawing;
using System.Xml.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace Props;
public class Props : BasePlugin
{
    public override string ModuleName => "Props";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "0.0.3";

    public static readonly string MessagePrefix = $"{ChatColors.White}[{ChatColors.Blue}Prop{ChatColors.White}] ";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            List<CCSPlayerController> playerlist = Utilities.GetPlayers();
            foreach (var player in playerlist)
            {
                if (!player.IsValid)
                {
                    continue;
                }
                player.PrintToChat($"{MessagePrefix} Reset score!");
                player.Score = 0;
                Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");
            }

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerHurt>((@event, info) =>
        {
            var hurtedPlayer = @event.Userid;
            var attacker = @event.Attacker;

            var weapon = @event.Weapon;
            var dmgHealth = @event.DmgHealth;
            var hitgroup = @event.Hitgroup;

            if (!attacker.IsValid || !hurtedPlayer.IsValid || hurtedPlayer.IsBot)
            {
                return HookResult.Continue;
            }

            if (hurtedPlayer.TeamNum == ((int)CsTeam.Terrorist))
            {
                DamageScore(attacker, dmgHealth);
            }

            return HookResult.Continue;
        });
    }

    [ConsoleCommand("prop", "Open menu")]
    [CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void menu(CCSPlayerController? player, CommandInfo command)
    {
        var menu = new ChatMenu("Props Menu - Your credits: " + player?.Score);
        var name = "soccer ball";
        var model = "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl";
        var cost = 500;
        var massScale = 10;
        menu.AddMenuOption(name, (player, option) => {

            propSpawner(player, command, name, model, cost, massScale);
        });
        name = "cube";
        menu.AddMenuOption(name, (player, option) => {
            model = "models/dev/dev_cube.vmdl";
            cost = 1000;
            massScale = 50000;
            propSpawner(player, command, name, model, cost, massScale);
        });
        name = "vending machine";
        menu.AddMenuOption(name, (player, option) => {
            model = "models/props/de_nuke/hr_nuke/nuke_vending_machine/nuke_vending_machine.vmdl";
            cost = 2000;
            massScale = 50000;
            propSpawner(player, command, name, model, cost, massScale);
        });
        MenuManager.OpenChatMenu(player, menu);
    }

    [ConsoleCommand("propscore", "Give 5000 points")]
    [RequiresPermissions("@css/cheats")]
    public void propPoints(CCSPlayerController? player, CommandInfo command)
    {
        var score = player.Score;
        player.Score = score + 5000;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");
    }

    public void propSpawner(CCSPlayerController? player, CommandInfo command, string name, string model, int cost, float massScale)
    {

        if (player.TeamNum != ((int)CsTeam.CounterTerrorist))
        {
            player.PrintToChat($" {MessagePrefix} You are on the wrong team, you can't buy a {ChatColors.Red}{name}{ChatColors.White}!");
            return;
        }

        if (player.Score < cost)
        {
            player.PrintToChat($" {MessagePrefix} You don't have money to buy a {ChatColors.Red}{name}{ChatColors.White}, need {ChatColors.Red}{cost} points{ChatColors.White}");
            return;
        }
        player.PrintToChat($" {MessagePrefix} You bought a {ChatColors.Red}{name}{ChatColors.White} for {ChatColors.Green}{cost} points{ChatColors.White}");
        player.Score -= cost;
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");

        var entity = Utilities.CreateEntityByName<CPhysicsPropMultiplayer>("prop_physics_override");

        if (entity == null || !entity.IsValid)
        {
            return;
        }

        var playerPos = new Vector(
                player.PlayerPawn.Value!.AbsOrigin!.X,
                player.PlayerPawn.Value!.AbsOrigin!.Y,
                player.PlayerPawn.Value!.AbsOrigin!.Z
            );

        var playerAngles = new QAngle(
                player.PlayerPawn.Value!.EyeAngles!.X,
                player.PlayerPawn.Value!.AbsRotation!.Y,
                player.PlayerPawn.Value!.AbsRotation!.Z
            );

        float[] output = new float[3];

        playerPos.Z += 50;

        AddInFrontOf(playerPos, playerAngles, 60.0f, output);

        entity.Teleport(
            new Vector(
                output[0],
                output[1],
                output[2]
            ),
            new QAngle(
                0.0f, 0.0f, 0.0f
            ),
            new Vector(
                0.0f, 0.0f, 0.0f
            )
        );

        entity.SetModel(model);
        entity.MassScale = massScale;
        entity.MaxHealth = 100;
        entity.TakesDamage = true;
        entity.Health = 100;

        Server.PrintToChatAll($" {MessagePrefix}{ChatColors.Green}{player.PlayerName}{ChatColors.White} bought a {ChatColors.Red}{name}{ChatColors.White}!");
        entity.DispatchSpawn();
    }

    private void AddInFrontOf(Vector vecOrigin, QAngle vecAngle, float units, float[] output)
    {
        float[] vecView = new float[3];
        GetViewVector(vecAngle, vecView);

        output[0] = vecView[0] * units + vecOrigin[0];
        output[1] = vecView[1] * units + vecOrigin[1];
        output[2] = vecView[2] * units + vecOrigin[2];
    }

    private void GetViewVector(QAngle vecAngle, float[] output)
    {
        output[0] = (float)Math.Cos(vecAngle.Y * (Math.PI / 180));
        output[1] = (float)Math.Sin(vecAngle.Y * (Math.PI / 180));
        output[2] = -(float)Math.Sin(vecAngle.X * (Math.PI / 180));
    }

    private void DamageScore(CCSPlayerController player, int dmgHealth)
    {
        var score = player.Score;
        player.Score = score + (dmgHealth / 10);
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_iScore");
    }
}