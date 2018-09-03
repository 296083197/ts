#ifndef __TRADE_API_B_H
#define __TRADE_API_B_H

#include <Windows.h>

#ifdef __cplusplus
extern "C" {
#endif

//
// 打开TDX
//
int WINAPI OpenTdx(char *pszErrInfo);

//
// 关闭TDX
//
void WINAPI CloseTdx();

//
// 登录帐号
//
int WINAPI Logon(
    const char* pszIP,
    short nPort,
    const char* pszVersion,
    short nYybID,
    const char* pszAccountNo,
    const char* pszTradeAccount,
    const char* pszJyPassword,
    const char* pszTxPassword,
    char* pszErrInfo);

//
// 注销
//
void WINAPI Logoff(int nClientID);

//
//
//
bool WINAPI IsConnectOK(int nClientID);

//
// 查询各类交易数据
//
int WINAPI QueryData(
    int nClientID,
    int nCategory,
    char* pszResult,
    char* pszErrInfo);

//
// 下单
//
int WINAPI SendOrder(
    int nClientID,
    int nCategory,
    int nPriceType,
    const char* pszGddm,
    const char* pszZqdm,
    float fPrice,
    int nQuantity,
    char* pszResult,
    char* pszErrInfo);

//
// 撤单
//
int WINAPI CancelOrder(
    int nClientID,
    char nMarket,
    const char* pszOrderID,
    char* pszResult,
    char* pszErrInfo);

//
// 获取五档报价
//
int WINAPI GetQuote(
    int nClientID,
    const char* pszZqdm,
    char* pszResult,
    char* pszErrInfo);

//
//
//
int WINAPI GetTradableQuantity(
    int nClientID,
    int nCategory,
    int nPriceType,
    const char* pszGddm,
    const char* pszZqdm,
    float fPrice,
    char* pszResult,
    char* pszErrInfo);

//
// 融资融券账户直接还款
//
int WINAPI Repay(
    int nClientID,
    const char* pszAmount,
    char* pszResult,
    char* pszErrInfo);

//
// 查询各类历史数据
//
int WINAPI QueryHistoryData(
    int nClientID,
    int nCategory,
    const char* pszStartDate,
    const char* pszEndDate,
    char* pszResult,
    char* pszErrInfo);

//
// 单账户批量查询各类交易数据
//
int WINAPI QueryDatas(
    int nClientID,
    int nCategory[],
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

//
// 单账户批量下单
//
int WINAPI SendOrders(
    int nClientID,
    int nCategory[],
    int nPriceType[],
    const char* pszGddm[],
    const char* pszZqdm[],
    float fPrice[],
    int nQuantity[],
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

//
// 单账户批量撤单
//
int WINAPI CancelOrders(
    int nClientID,
    const char nMarket[],
    const char* pszOrderID[],
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

//
// 单账户批量获取五档报价
//
int WINAPI GetQuotes(
    int nClientID,
    const char* pszZqdm[],
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

//
// 一键打新
//
int WINAPI QuickIPO(int nClientID);

//
// 一键打新
//
int WINAPI QuickIPODetail(
    int nClientID,
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

#ifdef __cplusplus
}
#endif

#endif
