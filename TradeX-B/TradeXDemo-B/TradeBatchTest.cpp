
#include "stdafx.h"

#include <iostream>
#include <string>
#include <vector>
#include <map>

using namespace std;

#include "TradeApi-B.h"


//
//
//

void SplitStrToVector(std::vector<std::string> &sVec, const std::string &sStr, const std::string &sSep)
{
    std::string::size_type i = 0;
    std::string::size_type j = sStr.find(sSep);

    while (j != std::string::npos)
    {
        sVec.push_back(sStr.substr(i, j-i));

        i = j + sSep.length();
        j = sStr.find(sSep, j + sSep.length());
    }

    if (j == std::string::npos)
        sVec.push_back(sStr.substr(i, sStr.length()));
}


class CTdxResult
{
public:
    std::map<std::string, int> m_mapHeader;

    std::vector<std::string> m_vecHeader;
    std::vector<std::vector<std::string> > m_vecContent;

public:
    CTdxResult(const std::string &sResult, const std::string &sLineSep = "\n", const std::string &sFieldSep = "\t")
    {
        std::vector<std::string> vecInfo;

        SplitStrToVector(vecInfo, sResult, sLineSep);

        if (vecInfo.empty())
            return;

        //
        //
        SplitStrToVector(m_vecHeader, vecInfo[0], sFieldSep);

        for (unsigned int i=0; i<m_vecHeader.size(); ++i)
        {
            char szBuff[10];

            if (m_vecHeader[i].empty())
            {
                sprintf(szBuff, "i%d", i);
                m_vecHeader[i] = szBuff;
            }

            m_mapHeader[m_vecHeader[i]] = i;
        }

        //
        //
        std::vector<std::string> vecFields;

        for (unsigned int i=1; i<vecInfo.size(); ++i)
        {
            vecFields.clear();

            SplitStrToVector(vecFields, vecInfo[i], sFieldSep);

            if (vecFields.size() < m_vecHeader.size())
                continue;

            m_vecContent.push_back(vecFields);
        }
    }

    int GetRowNum()
    {
        return m_vecContent.size();
    }

    int GetColNum()
    {
        return m_vecHeader.size();
    }

    int GetIndexByTitle(const std::string sTitle)
    {
        std::map<std::string, int>::iterator iter;

        iter = m_mapHeader.find(sTitle);
        if (iter == m_mapHeader.end())
            return -1;

        return iter->second;
    }

    std::string GetValue(int nRow, const std::string &sTitle)
    {
        if (nRow >= m_vecContent.size())
            return "";

        int nIndex = GetIndexByTitle(sTitle);
        if (nIndex < 0)
            return "";

        return m_vecContent[nRow][nIndex];
    }
};

//
//

class CTestTdx
{
public:
    CTestTdx(const char *pszHost,
             int nPort,
             const char *pszVersion,
             int nYydId,
             const char* pszAccountNo,
             const char* pszTradeAccount,
             const char* pszJyPassword,
             const char* pszTxPassword)
        : m_sHost(pszHost),
          m_sVersion(pszVersion),
          m_nPort(nPort),
          m_nYybId(nYydId),
          m_sAccountNo(pszAccountNo),
          m_sTradeAccount(pszTradeAccount),
          m_sJyPassword(pszJyPassword),
          m_sTxPassword(pszTxPassword)
    {
        m_nClientID = -1;

        memset(m_szResult, 0, sizeof(m_szResult));
        memset(m_szErrInfo, 0, sizeof(m_szErrInfo));
    }

    ~CTestTdx()
    {
        if (m_nClientID >= 0)
            XLogoff();
    }

    bool XLogin()
    {
        int nClientID;

        nClientID = Logon(m_sHost.c_str(),
                          m_nPort,
                          m_sVersion.c_str(),
                          m_nYybId,
                          m_sAccountNo.c_str(),
                          m_sTradeAccount.c_str(),
                          m_sJyPassword.c_str(),
                          m_sTxPassword.c_str(),
                          m_szErrInfo);
        if (nClientID < 0)
        {
            cout << "Login fail ! " << m_szErrInfo << endl;
            return 0;
        }

        m_nClientID = nClientID;

        cout << "Login to " << m_sAccountNo << " ok ! <" << m_nClientID << ">" << endl;
        cout << "\tConnectOK - " << IsConnectOK(nClientID) << endl;
        return 1;
    }

    bool XQueryData(int nCategory)
    {
        if (m_nClientID < 0)
        {
            cout << "<" << m_nClientID << "> QueryData fail !" << endl;
            return 0;
        }

        int nRet = QueryData(m_nClientID, nCategory, m_szResult, m_szErrInfo);
        if (nRet < 0)
        {
            cout << "<" << m_nClientID << "> QueryData fail ! " << endl;
            cout << m_szErrInfo << endl;
            return 0;
        }

        cout << "<" << m_nClientID << "> QueryData ok ! " << endl;
        cout << m_szResult << endl;
        return 1;
    }

    bool XShareHolder()
    {
        if (m_nClientID < 0)
        {
            cout << "<" << m_nClientID << "> QueryData fail !" << endl;
            return 0;
        }

        cout << "*** " << m_nClientID << endl;

        int nRet = QueryData(m_nClientID, 5, m_szResult, m_szErrInfo);
        if (nRet < 0)
        {
            cout << "<" << m_nClientID << "> QueryData fail !" << endl;
            cout << m_szErrInfo << endl;
            return 0;
        }

        //
        //

        CTdxResult share_holder(m_szResult);

        for (int i=0; i<share_holder.GetRowNum(); ++i)
        {
            std::string sAccountType = share_holder.GetValue(i, "帐号类别");

            if (sAccountType == "0")
            {
                m_sShareHolderSZ = share_holder.GetValue(i, "股东代码");
                cout << "<" << m_nClientID << "> 深圳-股东代码=" << m_sShareHolderSZ << endl;
            }
            else if (sAccountType == "1")
            {
                m_sShareHolderSH = share_holder.GetValue(i, "股东代码");
                cout << "<" << m_nClientID << "> 上海-股东代码=" << m_sShareHolderSH << endl;
            }
        }

        cout << "<" << m_nClientID << "> QueryData ok ! " << endl;
        cout << m_szResult << endl;
        return 1;
    }

    bool XSendOrder(int nMarket)
    {
        if (m_nClientID < 0)
        {
            cout << "<" << m_nClientID << "> SendOrder fail !" << endl;
            return 0;
        }

        if (nMarket)
        {
            cout << "<" << m_nClientID << "> 601988 市价 100股 ... " << endl;

            int nRet = SendOrder(m_nClientID, 0, 4, m_sShareHolderSH.c_str(), "601988", 0, 100, m_szResult, m_szErrInfo);
            if (nRet < 0)
            {
                cout << "<" << m_nClientID << "> SendOrder fail ! " << endl;
                cout << m_szErrInfo << endl;
            }
            else
            {
                cout << "<" << m_nClientID << "> SendOrder ok ! " << endl;
                cout << m_szResult << endl;
            }
        }
        else
        {
            cout << "<" << m_nClientID << "> 000001 市价 100股 ... " << endl;

            int nRet = SendOrder(m_nClientID, 0, 4, m_sShareHolderSZ.c_str(), "000001", 0, 100, m_szResult, m_szErrInfo);
            if (nRet < 0)
            {
                cout << "<" << m_nClientID << "> SendOrder fail ! " << endl;
                cout << m_szErrInfo << endl;
            }
            else
            {
                cout << "<" << m_nClientID << "> SendOrder ok ! " << endl;
                cout << m_szResult << endl;
            }
        }

        return 1;
    }

    void XLogoff()
    {
        if (m_nClientID < 0)
        {
            cout << "<" << m_nClientID << "> Logoff fail !" << endl;
            return;
        }

        Logoff(m_nClientID);

        cout << "<" << m_nClientID << "> Logoff !" << endl;

        m_nClientID = -1;
    }


private:

    std::string m_sHost;
    int m_nPort;

    std::string m_sVersion;

    int m_nYybId;
    int m_nAccountType;

    std::string m_sAccountNo;
    std::string m_sTradeAccount;
    std::string m_sJyPassword;
    std::string m_sTxPassword;

    int m_nClientID;

    std::string m_sShareHolderSZ;
    std::string m_sShareHolderSH;

    char m_szResult[0x8010];
    char m_szErrInfo[256];
};


#define MAX_BATCH_NUM   4

char g_szErrInfo[256];

int test_trade_batch_funcs()
{
    std::vector<CTestTdx *> vecTdx(MAX_BATCH_NUM);

    if (OpenTdx(g_szErrInfo) < 0)
    {
        std::cout << "error! " << g_szErrInfo << std::endl;
        return 0;
    }

    //
    //

    for (int i=0; i<MAX_BATCH_NUM; i++)
    {
        vecTdx[i] = new CTestTdx("mock.tdx.com.cn",
                                 7708,
                                 "6.40",
                                 9000,
                                 "net828@163.com",
                                 "001001001005792",
                                 "123123",
                                 "");
    }

    /*
    for (int i=0; i<MAX_BATCH_NUM; i++)
    {
        vecTdx[i] = new CTestTdx("61.132.54.83", // "wt.htsc.com.cn",
    	vecTdx[i] = new CTestTdx("wt.htsc.com.cn",
                                 7708,
                                 "6.40",
                                 0,
                                 "666622762660",
                                 "666622762660",
                                 "交易密码",
                                 "通信密码");
    }
    */

    //
    //

    cout << "\t 0 - Logon ...\n" << endl;

    for (int i=0; i<MAX_BATCH_NUM; i++)
        vecTdx[i]->XLogin();

    cout << endl;
    cout << "\t按回车键继续......\n";
    cin.get();

    //
    //
    cout << "\t 0 - ShareHolder ...\n" << endl;

    for (int i=0; i<MAX_BATCH_NUM; i++)
    {
        vecTdx[i]->XShareHolder();
        cout << endl;
    }

    cout << endl;
    cout << "\t按回车键继续......\n";
    cin.get();

    //
    //

    if (1)
    {
        cout << "\t 0 - 查询资金 QueryData(nClientID, 0, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(0);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 1 - 查询股份	 QueryData(nClientID, 1, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(1);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";

        //

        cout << "\t 2 - 查询当日委托 QueryData(nClientID, 2, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(2);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 3 - 查询当日成交 QueryData(nClientID, 3, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(3);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 4 - 查询可撤单	 QueryData(nClientID, 4, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(4);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 5 - 查询股东代码 QueryData(nClientID, 5, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(5);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();
    }

    //
    //

    if (0)
    {
        cout << "\t 12 - 可申购新股查询 QueryData(nClientID, 12, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(12);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 13 - 新股申购额度查询 QueryData(nClientID, 13, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(13);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 14 - 配号查询 QueryData(nClientID, 14, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(14);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        //

        cout << "\t 15 - 中签查询 QueryData(nClientID, 15, Result, ErrInfo)\n" << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XQueryData(15);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();
    }

    //
    //

    if (1)
    {
        cout << "\t 4 - SendOrder 深市买入 ... " << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XSendOrder(0);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();

        cout << "\t 4 - SendOrder 沪市买入 ... " << endl;

        for (int i=0; i<MAX_BATCH_NUM; i++)
        {
            vecTdx[i]->XSendOrder(1);
            cout << endl;
        }

        cout << endl;
        cout << "\t按回车键继续......\n";
        cin.get();
    }

    //
    //

    cout << "\t 7 - Logoff ...\n" << endl;

    for (int i=0; i<MAX_BATCH_NUM; i++)
        vecTdx[i]->XLogoff();

    cout << endl;
    cout << "\t按回车键继续......\n";
    cin.get();


    //
    // 析构
    //

    for (int i=0; i<MAX_BATCH_NUM; i++)
        delete vecTdx[i];

    //
    //
    CloseTdx();

    return 1;
}


