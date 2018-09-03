

#include "stdafx.h"

#include "TradeX.h"

#include <iostream>

using namespace std;

#define E1  1 // TdxExHq_GetMarkets
#define E2  1 // TdxExHq_GetInstrumentCount
#define E3  1 // TdxExHq_GetInstrumentInfo
#define E4  1 // TdxExHq_GetInstrumentQuote
#define E5  1 // TdxExHq_GetInstrumentBars
#define E6  1 // TdxExHq_GetMinuteTimeData
#define E7  1 // TdxExHq_GetHistoryMinuteTimeData
#define E8  1 // TdxExHq_GetTransactionData
#define E9  1 // TdxExHq_GetHistoryTransactionData

int test_exhq_batch_funcs(const char *pszHqSvrIP, short nPort)
{
    //��ʼ��ȡ��������
    char* Result = new char[1024 * 1024];
    char* ErrInfo = new char[256];
    short Count = 10;

    //���ӷ�����
    int nConn = TdxExHq_Connect(pszHqSvrIP, nPort, Result, ErrInfo);
    if (nConn < 0)
    {
        cout << ErrInfo << endl;
        return 0;
    }

    std::cout << Result << std::endl;

    bool bool1;

#if E1
    {
        cout << "\n*** TdxExHq_GetMarkets\n";

        bool1 = TdxExHq_GetMarkets(nConn, Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;//����ʧ��
            getchar();
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

#if E2
    {
        cout << "\n*** TdxExHq_GetInstrumentCount\n";

        int nCount = -1;
        bool1 = TdxExHq_GetInstrumentCount(nConn, &nCount, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;//����ʧ��
            getchar();
            return 0;
        }

        cout << "Count = " << nCount << endl;
        getchar();
    }
#endif

#if E3
    {
        cout << "\n*** TdxExHq_GetInstrumentInfo\n";

        Count = 50;
        bool1 = TdxExHq_GetInstrumentInfo(nConn, 0, &Count,  Result, ErrInfo); //Count���511
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << "Count = " << Count << endl;
        cout << Result << endl;
        getchar();
    }
#endif

#if E4
    {
        cout << "\n*** TdxExHq_GetInstrumentQuote\n";

        bool1 = TdxExHq_GetInstrumentQuote(nConn, 47, "IF1702",  Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

#if E5
    {
        cout << "\n*** TdxExHq_GetInstrumentBars\n";

        // 0->5����K��    1->15����K��    2->30����K��  3->1СʱK��    4->��K��  5->��K��  6->��K��  7->1����  8->1����K��  9->��K��  10->��K��  11->��K��
        Count = 50;
        bool1 = TdxExHq_GetInstrumentBars(nConn, 7, 539, "HSI", 0, &Count, Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

#if E6
    {
        cout << "\n*** TdxExHq_GetMinuteTimeData\n";

        bool1 = TdxExHq_GetMinuteTimeData(nConn, 47, "IF1702", Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

#if E7
    {
        cout << "\n*** TdxExHq_GetHistoryMinuteTimeData\n";

        bool1 = TdxExHq_GetHistoryMinuteTimeData(nConn, 47, "IF1702", 20170113, Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

#if E8
    {
        cout << "\n*** TdxExHq_GetTransactionData\n";

        Count = 200;
        bool1 = TdxExHq_GetTransactionData(nConn, 47, "IF1702", 0, &Count, Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

#if E9
    {
        cout << "\n*** TdxExHq_GetHistoryTransactionData\n";

        bool1 = TdxExHq_GetHistoryTransactionData(nConn, 47, "IF1702", 20170113, 0, &Count, Result, ErrInfo);
        if (!bool1)
        {
            cout << ErrInfo << endl;
            return 0;
        }

        cout << Result << endl;
        getchar();
    }
#endif

    TdxExHq_Disconnect(nConn);

    cout << "�Ѿ��Ͽ����������" << endl;

    getchar();
    return 0;
}


