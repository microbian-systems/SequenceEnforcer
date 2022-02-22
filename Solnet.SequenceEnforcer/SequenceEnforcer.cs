using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solnet;
using Solnet.Programs.Abstract;
using Solnet.Programs.Utilities;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Core.Sockets;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using SequenceEnforcer;
using SequenceEnforcer.Program;
using SequenceEnforcer.Accounts;
using SequenceEnforcer.Errors;

namespace SequenceEnforcer
{
    namespace Accounts
    {
        public class SequenceAccount
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 5679986792759222136UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[] { 120, 223, 82, 231, 228, 93, 211, 78 };
            public static string ACCOUNT_DISCRIMINATOR_B58 => "MDca7Tv86gV";
            public ulong SequenceNum { get; set; }

            public PublicKey Authority { get; set; }

            public static SequenceAccount Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                SequenceAccount result = new SequenceAccount();
                result.SequenceNum = _data.GetU64(offset);
                offset += 8;
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }
    }

    namespace Errors
    {
    }

    public class SequenceEnforcerClient : BaseClient
    {
        public SequenceEnforcerClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient) : base(rpcClient, streamingRpcClient)
        {
            RpcClient = rpcClient;
            StreamingRpcClient = streamingRpcClient;
        }

        public async Task<Solnet.Programs.Models.ProgramAccountsResultWrapper<List<SequenceAccount>>> GetSequenceAccountsAsync(string programAddress = "GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp", Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solnet.Rpc.Models.MemCmp> { new Solnet.Rpc.Models.MemCmp { Bytes = SequenceAccount.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 } };
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solnet.Programs.Models.ProgramAccountsResultWrapper<List<SequenceAccount>>(res);
            List<SequenceAccount> resultingAccounts = new List<SequenceAccount>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => SequenceAccount.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solnet.Programs.Models.ProgramAccountsResultWrapper<List<SequenceAccount>>(res, resultingAccounts);
        }

        public async Task<Solnet.Programs.Models.AccountResultWrapper<SequenceAccount>> GetSequenceAccountAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solnet.Programs.Models.AccountResultWrapper<SequenceAccount>(res);
            var resultingAccount = SequenceAccount.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solnet.Programs.Models.AccountResultWrapper<SequenceAccount>(res, resultingAccount);
        }

        public async Task<RequestResult<string>> SendInitializeAsync(InitializeAccounts accounts, byte bump, string sym, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId = null)
        {
            programId ??= new("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
            Solnet.Rpc.Models.TransactionInstruction instr = Program.SequenceEnforcerProgram.Initialize(accounts, bump, sym, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendResetSequenceNumberAsync(ResetSequenceNumberAccounts accounts, ulong sequenceNum, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId = null)
        {
            programId ??= new("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
            Solnet.Rpc.Models.TransactionInstruction instr = Program.SequenceEnforcerProgram.ResetSequenceNumber(accounts, sequenceNum, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendCheckAndSetSequenceNumberAsync(CheckAndSetSequenceNumberAccounts accounts, ulong sequenceNum, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId = null)
        {
            programId ??= new("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
            Solnet.Rpc.Models.TransactionInstruction instr = Program.SequenceEnforcerProgram.CheckAndSetSequenceNumber(accounts, sequenceNum, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }
    }

    namespace Program
    {
        public class InitializeAccounts
        {
            public PublicKey SequenceAccount { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ResetSequenceNumberAccounts
        {
            public PublicKey SequenceAccount { get; set; }

            public PublicKey Authority { get; set; }
        }

        public class CheckAndSetSequenceNumberAccounts
        {
            public PublicKey SequenceAccount { get; set; }

            public PublicKey Authority { get; set; }
        }

        public static class SequenceEnforcerProgram
        {
            public static Solnet.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, byte bump, string sym, PublicKey programId = null)
            {
                programId ??= new("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
                List<Solnet.Rpc.Models.AccountMeta> keys = new()
                { Solnet.Rpc.Models.AccountMeta.Writable(accounts.SequenceAccount, false), Solnet.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true), Solnet.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false) };
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                _data.WriteU8(bump, offset);
                offset += 1;
                offset += _data.WriteBorshString(sym, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solnet.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solnet.Rpc.Models.TransactionInstruction ResetSequenceNumber(ResetSequenceNumberAccounts accounts, ulong sequenceNum, PublicKey programId = null)
            {
                programId ??= new("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
                List<Solnet.Rpc.Models.AccountMeta> keys = new()
                { Solnet.Rpc.Models.AccountMeta.Writable(accounts.SequenceAccount, false), Solnet.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true) };
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(11014749699822031972UL, offset);
                offset += 8;
                _data.WriteU64(sequenceNum, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solnet.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }

            public static Solnet.Rpc.Models.TransactionInstruction CheckAndSetSequenceNumber(CheckAndSetSequenceNumberAccounts accounts, ulong sequenceNum, PublicKey programId = null)
            {
                programId ??= new("GDDMwNyyx8uB6zrqwBFHjLLG3TBYk2F8Az4yrQC5RzMp");
                List<Solnet.Rpc.Models.AccountMeta> keys = new()
                { Solnet.Rpc.Models.AccountMeta.Writable(accounts.SequenceAccount, false), Solnet.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true) };
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(18443923197421745496UL, offset);
                offset += 8;
                _data.WriteU64(sequenceNum, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solnet.Rpc.Models.TransactionInstruction { Keys = keys, ProgramId = programId.KeyBytes, Data = resultData };
            }
        }
    }
}

