#ifndef __L2HQ_API_H
#define __L2HQ_API_H

#include <Windows.h>

#ifdef __cplusplus
extern "C" {
#endif

//
// ����API
//

//1.����API����TdxHqApi.dll�ļ��ĵ����������������º�����(�������麯����Ϊ�ͻ������������ѯ�����Ƿ���������)

//2.APIʹ������Ϊ: Ӧ�ó����ȵ���TdxL2Hq_Connect����ͨ�������������,Ȼ��ſ��Ե��������ӿڻ�ȡ��������,Ӧ�ó���Ӧ���д��������������, �ӿ����̰߳�ȫ��

//3.������������˵��

//
//  ����ͨ�������������,��������ַ����ȯ�������¼�����е�ͨѶ�����в��
//
// <param name="pszIP">������IP</param>
// <param name="nPort">�������˿�</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�������������ID, ʧ�ܷ���0</returns>
//
int WINAPI TdxL2Hq_Connect(
    const char *pszIP,
    short nPort,
    const char *pszL2User,
    const char *pszL2Password,
    char *pszResult,
    char *pszErrInfo);


//
// �Ͽ�ͬ������������
//
void WINAPI TdxL2Hq_Disconnect(int nConnID);


//
//
//
void WINAPI TdxL2Hq_SetTimeout(
    int nConnID,
    int nReadTimeout,
    int nWriteTimeout);


//
// ��ȡָ���г��ڵ�֤ȯ��Ŀ
//
bool WINAPI TdxL2Hq_GetSecurityCount(
    int nConnID,
    char nMarket,
    short *pnCount,
    char *pszErrInfo);


//
// ��ȡָ���г��ڵ�֤ȯ�б�
//
bool WINAPI TdxL2Hq_GetSecurityList(
    int nConnID,
    char nMarket,
    short nStart,
    short *pnCount,
    char *pszResult,
    char *pszErrInfo);


//
// ������ȡ���֤ȯ���嵵��������
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�, ��i��Ԫ�ر�ʾ��i��֤ȯ���г�����</param>
// <param name="pszZqdm">֤ȯ����, Count��֤ȯ������ɵ�����</param>
// <param name="nCount">APIִ��ǰ,��ʾ�û�Ҫ�����֤ȯ��Ŀ,���290, APIִ�к�,������ʵ�ʷ��ص���Ŀ</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetSecurityQuotes(
    int nConnID,
    const char nMarket[],
    const char *pszZqdm[],
    short *pnCount,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ֤ȯ��K������
//
// <param name="nCategory">K������, 0->5����K��    1->15����K��    2->30����K��  3->1СʱK��    4->��K��  5->��K��  6->��K��  7->1����  8->1����K��  9->��K��  10->��K��  11->��K��< / param>
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="nStart">K�߿�ʼλ��,���һ��K��λ����0, ǰһ����1, ��������</param>
// <param name="nCount">APIִ��ǰ,��ʾ�û�Ҫ�����K����Ŀ, APIִ�к�,������ʵ�ʷ��ص�K����Ŀ, ���ֵ800</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetSecurityBars(
    int nConnID,
    char nCategory,
    char nMarket,
    const char *pszZqdm,
    short nStart,
    short *pnCount,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡָ����K������
//
// <!-- <param name="nCategory"> -->K������, 0->5����K��    1->15����K��    2->30����K��  3->1СʱK��    4->��K��  5->��K��  6->��K��  7->1����  8->1����K��  9->��K��  10->��K��  11->��K��< / param>
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="nStart">K�߿�ʼλ��,���һ��K��λ����0, ǰһ����1, ��������</param>
// <param name="nCount">APIִ��ǰ,��ʾ�û�Ҫ�����K����Ŀ, APIִ�к�,������ʵ�ʷ��ص�K����Ŀ,���ֵ800</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetIndexBars(
    int nConnID,
    char nCategory,
    char nMarket,
    const char *pszZqdm,
    short nStart,
    short *pnCount,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ��ʱ����
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetMinuteTimeData(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ��ʷ��ʱ����
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="nDate">����, ����2014��1��1��Ϊ����20140101</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetHistoryMinuteTimeData(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    int nDate,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ��ʱ�ɽ�����
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="nStart">K�߿�ʼλ��,���һ��K��λ����0, ǰһ����1, ��������</param>
// <param name="nCount">APIִ��ǰ,��ʾ�û�Ҫ�����K����Ŀ, APIִ�к�,������ʵ�ʷ��ص�K����Ŀ</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetTransactionData(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    short nStart,
    short *pnCount,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ��ʷ��ʱ�ɽ�����
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="nStart">K�߿�ʼλ��,���һ��K��λ����0, ǰһ����1, ��������</param>
// <param name="nCount">APIִ��ǰ,��ʾ�û�Ҫ�����K����Ŀ, APIִ�к�,������ʵ�ʷ��ص�K����Ŀ</param>
// <param name="nDate">����, ����2014��1��1��Ϊ����20140101</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetHistoryTransactionData(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    short nStart,
    short *pnCount,
    int date,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡF10���ϵķ���
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����, ��ʽΪ������ݣ�������֮��ͨ��\n�ַ��ָ������֮��ͨ��\t�ָ���һ��Ҫ����1024*1024�ֽڵĿռ䡣����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetCompanyInfoCategory(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡF10���ϵ�ĳһ���������
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="pszFileName">��Ŀ���ļ���, ��TdxL2Hq_GetCompanyInfoCategory������Ϣ�л�ȡ</param>
// <param name="nStart">��Ŀ�Ŀ�ʼλ��, ��TdxL2Hq_GetCompanyInfoCategory������Ϣ�л�ȡ</param>
// <param name="nLength">��Ŀ�ĳ���, ��TdxL2Hq_GetCompanyInfoCategory������Ϣ�л�ȡ</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����,����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetCompanyInfoContent(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    const char *pszFileName,
    int nStart,
    int nLength,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ��Ȩ��Ϣ��Ϣ
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����,����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetXDXRInfo(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    char *pszResult,
    char *pszErrInfo);


//
// ��ȡ������Ϣ
//
// <param name="nMarket">�г�����,   0->����     1->�Ϻ�</param>
// <param name="pszZqdm">֤ȯ����</param>
// <param name="pszResult">��APIִ�з��غ�Result�ڱ����˷��صĲ�ѯ����,����ʱΪ���ַ�����</param>
// <param name="pszErrInfo">��APIִ�з��غ�������������˴�����Ϣ˵����һ��Ҫ����256�ֽڵĿռ䡣û����ʱΪ���ַ�����</param>
//
// <returns>�ɹ�����true, ʧ�ܷ���false</returns>
//
bool WINAPI TdxL2Hq_GetFinanceInfo(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    char *pszResult,
    char *pszErrInfo);

//
// ��ȡʮ������
//
bool WINAPI TdxL2Hq_GetSecurityQuotes10(
    int nConnID,
    const char nMarket[],
    const char *pszZqdm[],
    short *pnCount,
    char* pszResult,
    char* pszErrInfo);

//
// ��ȡ���ί��(�Ӻ���ǰ)
//
bool WINAPI TdxL2Hq_GetDetailTransactionData(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    int nStart,
    short *pnCount,
    char* pszResult,
    char* pszErrInfo);

//
// ��ȡ���ί��(��ǰ����)
//
bool WINAPI TdxL2Hq_GetDetailTransactionDataEx(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    int nStart,
    short *pnCount,
    char* pszResult,
    char* pszErrInfo);

//
// ��ȡ���ί��(�Ӻ���ǰ)
//
bool WINAPI TdxL2Hq_GetDetailOrderData(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    int nStart,
    short *pnCount,
    char* pszResult,
    char* pszErrInfo);

//
// ��ȡ���ί��(��ǰ����)
//
bool WINAPI TdxL2Hq_GetDetailOrderDataEx(
    int nConnID,
    char nMarket,
    const char *pszZqdm,
    int nStart,
    short *pnCount,
    char* pszResult,
    char* pszErrInfo);

//
// ��ȡʮ������
//
bool WINAPI TdxL2Hq_GetBuySellQueue(
    int nConnID,
    char nMarket,
    const char* pszZqdm,
    char* pszResult,
    char* pszErrInfo);

#ifdef __cplusplus
}
#endif

#endif
