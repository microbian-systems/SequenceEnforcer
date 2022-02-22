# Solnet.SequenceEnforcer

SequenceEnforcer repository with automatic code generated.

Solnet.SequenceEnforcer.Examples contains small exemple how to create and submit instructions.

Note: all the code in Solnet.SequenceEnforcer is generated. For this specific case, a few optimizations could be done:
* In the SequenceEnforcerProgram methods, allocate a smaller _data buffer with stackalloc (these ixs are 80 bytes for set & reset, 105+Encoding.UTF8.GetByteCount(sym) bytes for acc initialization)
* ResetSequenceNumberAccounts & CheckAndSetSequenceNumberAccounts could be collapsed into single class
* Removal of SequenceEnforcerClient.SendCheckAndSetSequenceNumberAsync if not used

# Requirements

This code was generated using net6.0, but should work as is in net5.0.



# Support

Consider supporting us:

Sol Address: **oaksGKfwkFZwCniyCF35ZVxHDPexQ3keXNTiLa7RCSp**
[Mango Ref Link](https://trade.mango.markets/?ref=MangoSharp)



## Contributors

* **Hugo** - *Maintainer* - [murlokito](https://github.com/murlokito) [@hoakbuilds](twitter.com/hoakbuilds)
* **Tiago** - *Maintainer* - [tiago](https://github.com/tiago18c) [@qtmoses](twitter.com/qtmoses)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/bmresearch/Solnet.Serum/blob/master/LICENSE) file for details