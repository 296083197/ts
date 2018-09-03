#ifndef __TRADE_API_B_H
#define __TRADE_API_B_H

#include <Windows.h>

#ifdef __cplusplus
extern "C" {
#endif

//
// ��TDX
//
int WINAPI OpenTdx(char *pszErrInfo);

//
// �ر�TDX
//
void WINAPI CloseTdx();

//
// ��¼�ʺ�
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
// ע��
//
void WINAPI Logoff(int nClientID);

//
//
//
bool WINAPI IsConnectOK(int nClientID);

//
// ��ѯ���ཻ������
//
int WINAPI QueryData(
    int nClientID,
    int nCategory,
    char* pszResult,
    char* pszErrInfo);

//
// �µ�
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
// ����
//
int WINAPI CancelOrder(
    int nClientID,
    char nMarket,
    const char* pszOrderID,
    char* pszResult,
    char* pszErrInfo);

//
// ��ȡ�嵵����
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
// ������ȯ�˻�ֱ�ӻ���
//
int WINAPI Repay(
    int nClientID,
    const char* pszAmount,
    char* pszResult,
    char* pszErrInfo);

//
// ��ѯ������ʷ����
//
int WINAPI QueryHistoryData(
    int nClientID,
    int nCategory,
    const char* pszStartDate,
    const char* pszEndDate,
    char* pszResult,
    char* pszErrInfo);

//
// ���˻�������ѯ���ཻ������
//
int WINAPI QueryDatas(
    int nClientID,
    int nCategory[],
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

//
// ���˻������µ�
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
// ���˻���������
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
// ���˻�������ȡ�嵵����
//
int WINAPI GetQuotes(
    int nClientID,
    const char* pszZqdm[],
    int nCount,
    char* pszResultOK[],
    char* pszResultFail[],
    char* pszErrInfo);

//
// һ������
//
int WINAPI QuickIPO(int nClientID);

//
// һ������
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
