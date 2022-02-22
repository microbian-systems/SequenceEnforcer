using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SequenceEnforcer;
using SequenceEnforcer.Program;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

namespace Solnet.SequenceEnforcer.Examples
{
    public class Example
    {
        public static void Main(string[] _)
        {
            var program = new PublicKey("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");


#warning need wallet here to work
            var mn = "insert mnemonic here";
            var wallet = new Wallet.Wallet(mn);


            var rpc = Solnet.Rpc.ClientFactory.GetClient(Rpc.Cluster.MainNet);

            //creating client to be able to fetch accounts or effortlessly create accounts
            var client = new SequenceEnforcerClient(rpc, null);



            // PDA DERIVATION FOR SequenceEnforcer program accounts
            // generate PDA, the order of seeds:
            //  1-"sym"
            //  2-owner key
            // I guess in a market making environment, sym is the name/address of the market, 
            // and you use one sequence state acc per market to avoid resetting multiples when a single one fails?!
            var sym = "SOL/USDC";
            PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes(sym), wallet.Account.PublicKey.KeyBytes }, program, out var accAddress, out var bump);

            var initAccounts = new InitializeAccounts
            {
                Authority = wallet.Account.PublicKey,
                SequenceAccount = accAddress,
                SystemProgram = SystemProgram.ProgramIdKey
            };


            //creates & initializes the account the sequence account
            var res = client.SendInitializeAsync(initAccounts, bump, sym, wallet.Account,
                (payload, pk) => wallet.Sign(payload)).Result;

            var acc = client.GetSequenceAccountAsync(accAddress).Result;

            Console.WriteLine($"Current Seq number {acc.ParsedResult.SequenceNum}");

            // given that the sequence enforcer program utility is to enforce actual sequences in txs,
            // the remaining client methods are mostly useless as the instructions are useful in composed transactions instead of standalone


            //crafting ixs

            var advanceIx = SequenceEnforcerProgram.CheckAndSetSequenceNumber(accounts: new CheckAndSetSequenceNumberAccounts()
            {
                Authority = wallet.Account.PublicKey,
                SequenceAccount = accAddress
            }, sequenceNum: 12345ul);


            var resetx = SequenceEnforcerProgram.ResetSequenceNumber(accounts: new ResetSequenceNumberAccounts()
            {
                Authority = wallet.Account.PublicKey,
                SequenceAccount = accAddress
            }, sequenceNum: 12345ul);


            var recent = rpc.GetRecentBlockHash();

            var tx = new TransactionBuilder()
                .SetFeePayer(wallet.Account.PublicKey)
                .AddInstruction(advanceIx)
                .AddInstruction(MemoProgram.NewMemoV2("Composed tx with sequence control")) // imagine this is a mango place perp order ix instead
                .SetRecentBlockHash(recent.Result.Value.Blockhash)
                .Build(wallet.Account);

            var txResult = rpc.SendTransaction(tx);

            //next step:
            // still need to add error generation, so we could get & decode error message from SequenceEnforcer instructions
        }
    }
}
